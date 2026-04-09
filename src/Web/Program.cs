using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var configuredConnectionString = builder.Configuration.GetConnectionString("AppDb")
    ?? throw new InvalidOperationException("Connection string 'AppDb' was not found.");

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

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
