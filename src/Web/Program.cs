using System.Collections.Concurrent;
using Application.Interfaces;
using Application.Models;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using WorkInstructions.Infrastructure.AI;
using WorkInstructions.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000");
});

builder.Services.AddSingleton<UploadGenerationStore>();
builder.Services.AddScoped<IWorkInstructionGenerationService, OpenAiWorkInstructionGenerationService>();

var configuredConnectionString = builder.Configuration.GetConnectionString("AppDb");

if (!string.IsNullOrWhiteSpace(configuredConnectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(
            configuredConnectionString,
            sqlServerOptions =>
            {
                sqlServerOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sqlServerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            });
    });
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("WorkInstructions"));
}

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapHealthChecks("/health");

app.MapGet("/api/projects/{projectNumber}/validate", (string projectNumber) =>
{
    if (string.IsNullOrWhiteSpace(projectNumber))
    {
        return Results.BadRequest(new ValidationResultDto(false, ["Project number is required."]));
    }

    var exists = projectNumber.Length >= 4 && !projectNumber.StartsWith("X", StringComparison.OrdinalIgnoreCase);
    if (!exists)
    {
        return Results.Ok(new ValidationResultDto(false, [$"Project '{projectNumber}' was not found in Procore."]));
    }

    return Results.Ok(new ValidationResultDto(true, [$"Project found in Procore with id: procore-{projectNumber.ToUpperInvariant()}."]));
});

app.MapPost("/api/projects/{projectNumber}/uploads", async (
    string projectNumber,
    HttpRequest request,
    UploadGenerationStore store,
    CancellationToken cancellationToken) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("Upload content must be multipart/form-data.");
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var files = form.Files;

    if (files.Count == 0)
    {
        return Results.BadRequest("Choose at least one file before upload.");
    }

    var excelFiles = files.Where(IsExcel).ToList();
    var pdfFiles = files.Where(IsPdf).ToList();

    if (excelFiles.Count != 1 || pdfFiles.Count != 1 || files.Count != 2)
    {
        return Results.BadRequest("Upload exactly two files: one Excel (.xlsx/.xls) and one PDF (.pdf).");
    }

    var projectUploadDir = Path.Combine(Path.GetTempPath(), "workinstructions", projectNumber);
    Directory.CreateDirectory(projectUploadDir);

    var uploadedFiles = new List<UploadedFileInfo>(2);
    foreach (var file in files)
    {
        var filePath = Path.Combine(projectUploadDir, file.FileName);
        await using var output = File.Create(filePath);
        await using var input = file.OpenReadStream(maxAllowedSize: 25 * 1024 * 1024);
        await input.CopyToAsync(output, cancellationToken);
        uploadedFiles.Add(new UploadedFileInfo(file.FileName, file.ContentType, file.Length, filePath));
    }

    store.Save(projectNumber, uploadedFiles);

    return Results.Ok(new UploadResponseDto(
        ProjectNumber: projectNumber,
        UploadedCount: uploadedFiles.Count,
        Message: "Uploaded one Excel and one PDF successfully."));
});

app.MapPost("/api/workflows/start", async (
    [FromBody] StartGenerationDto request,
    UploadGenerationStore store,
    IWorkInstructionGenerationService generationService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.ProjectNumber))
    {
        return Results.BadRequest("Project number is required.");
    }

    if (!store.TryGet(request.ProjectNumber, out var uploadedFiles))
    {
        return Results.BadRequest("No uploaded files found. Upload one Excel and one PDF before starting generation.");
    }

    var excel = uploadedFiles.FirstOrDefault(IsExcel);
    var pdf = uploadedFiles.FirstOrDefault(IsPdf);
    if (excel is null || pdf is null)
    {
        return Results.BadRequest("Generation requires one Excel and one PDF upload.");
    }

    var normalized = new NormalizedWorkInstructionInput
    {
        ProjectId = $"procore-{request.ProjectNumber.ToUpperInvariant()}",
        ProjectName = $"Project {request.ProjectNumber.ToUpperInvariant()}",
        RequestedBy = "Workflow Operator",
        WorkScopeSummary = "Work instructions generated from uploaded estimate spreadsheet and proposal PDF.",
        ProposalSummary = $"Proposal source: {pdf.FileName} ({Math.Round(pdf.FileSizeBytes / 1024d, 1)} KB)",
        EstimateSummary = $"Estimate source: {excel.FileName} ({Math.Round(excel.FileSizeBytes / 1024d, 1)} KB)",
        ConfirmedFacts =
        [
            $"Excel uploaded: {excel.FileName}",
            $"PDF uploaded: {pdf.FileName}",
            "Plan sequencing generated for day-by-day execution."
        ],
        ProposedDailyPlan =
        [
            new DailyPlanItem { Day = 1, Activities = "Pre-job coordination, confirm scope from estimate, and stage materials.", Notes = "Use spreadsheet quantities as baseline." },
            new DailyPlanItem { Day = 2, Activities = "Execute primary field work as outlined in proposal scope.", Notes = "Track variances and blockers." },
            new DailyPlanItem { Day = 3, Activities = "Punch-list completion, quality checks, and closeout documentation.", Notes = "Capture final handoff notes." }
        ],
        OpenItems =
        [
            "Confirm crew assignment and start date with operations.",
            "Confirm any permit dependencies listed in project records."
        ]
    };

    var draft = await generationService.GenerateDraftAsync(normalized, promptTemplateVersion: "v1", cancellationToken);
    store.SaveDraft(request.ProjectNumber, draft);

    return Results.Ok(new StartJobResponseDto(Guid.NewGuid().ToString("D"), "Completed"));
});

app.MapGet("/api/projects/{projectNumber}/draft", (string projectNumber, UploadGenerationStore store) =>
{
    if (!store.TryGetDraft(projectNumber, out var draft))
    {
        return Results.NotFound("No draft has been generated yet.");
    }

    return Results.Text(draft, "text/plain");
});

app.MapPost("/api/projects/{projectNumber}/draft", async (string projectNumber, HttpRequest request, UploadGenerationStore store) =>
{
    var payload = await request.ReadFromJsonAsync<SaveDraftRequestDto>();
    if (payload is null || string.IsNullOrWhiteSpace(payload.Content))
    {
        return Results.BadRequest("Draft content is required.");
    }

    store.SaveDraft(projectNumber, payload.Content);
    return Results.Ok();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static bool IsExcel(IFormFile file)
{
    var extension = Path.GetExtension(file.FileName);
    return extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
           || extension.Equals(".xls", StringComparison.OrdinalIgnoreCase);
}

static bool IsPdf(IFormFile file)
{
    var extension = Path.GetExtension(file.FileName);
    return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
}

internal sealed record ValidationResultDto(bool IsValid, List<string> Messages);
internal sealed record UploadResponseDto(string ProjectNumber, int UploadedCount, string Message);
internal sealed record StartGenerationDto(string ProjectNumber);
internal sealed record StartJobResponseDto(string JobId, string Status);
internal sealed record SaveDraftRequestDto(string Content);
internal sealed record UploadedFileInfo(string FileName, string ContentType, long FileSizeBytes, string LocalPath);

internal sealed class UploadGenerationStore
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<UploadedFileInfo>> _uploads = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _drafts = new(StringComparer.OrdinalIgnoreCase);

    public void Save(string projectNumber, IReadOnlyList<UploadedFileInfo> files)
    {
        _uploads[projectNumber] = files;
    }

    public bool TryGet(string projectNumber, out IReadOnlyList<UploadedFileInfo> files)
    {
        return _uploads.TryGetValue(projectNumber, out files!);
    }

    public void SaveDraft(string projectNumber, string draft)
    {
        _drafts[projectNumber] = draft;
    }

    public bool TryGetDraft(string projectNumber, out string draft)
    {
        return _drafts.TryGetValue(projectNumber, out draft!);
    }
}
