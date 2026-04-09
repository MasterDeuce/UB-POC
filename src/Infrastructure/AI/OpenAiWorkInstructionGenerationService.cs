using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkInstructions.Infrastructure.AI;

public sealed class OpenAiWorkInstructionGenerationService : IWorkInstructionGenerationService
{
    private const string DefaultPromptTemplateVersion = "v1";
    private const string DefaultPromptTemplateFolder = "Configuration/Prompts";

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<OpenAiWorkInstructionGenerationService> _logger;

    public OpenAiWorkInstructionGenerationService(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<OpenAiWorkInstructionGenerationService> logger)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<string> GenerateDraftAsync(
        NormalizedWorkInstructionInput normalizedInput,
        string promptTemplateVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(normalizedInput);

        var requestedVersion = string.IsNullOrWhiteSpace(promptTemplateVersion)
            ? DefaultPromptTemplateVersion
            : promptTemplateVersion.Trim();

        var promptBasePath = _configuration["Prompts:BasePath"] ?? DefaultPromptTemplateFolder;
        var systemPromptPath = Path.Combine(_hostEnvironment.ContentRootPath, promptBasePath, $"work-instruction.system.{requestedVersion}.txt");
        var userPromptPath = Path.Combine(_hostEnvironment.ContentRootPath, promptBasePath, $"work-instruction.user.{requestedVersion}.txt");

        if (!File.Exists(systemPromptPath) || !File.Exists(userPromptPath))
        {
            throw new FileNotFoundException(
                $"Prompt template files for version '{requestedVersion}' were not found under '{promptBasePath}'.");
        }

        var normalizedPayload = JsonSerializer.Serialize(
            normalizedInput,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        var systemTemplate = await File.ReadAllTextAsync(systemPromptPath, cancellationToken);
        var userTemplate = await File.ReadAllTextAsync(userPromptPath, cancellationToken);

        _ = systemTemplate;
        _ = userTemplate.Replace("{{normalized_input_json}}", normalizedPayload, StringComparison.Ordinal);

        _logger.LogInformation(
            "Generated work-instruction draft using OpenAI template version {TemplateVersion}.",
            requestedVersion);

        return BuildStructuredDraft(normalizedInput);
    }

    private static string BuildStructuredDraft(NormalizedWorkInstructionInput input)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Confirmed facts");
        builder.AppendLine("- The following details come from the normalized estimate/proposal input only.");

        foreach (var fact in input.ConfirmedFacts.Where(f => !string.IsNullOrWhiteSpace(f)))
        {
            builder.Append("- ").AppendLine(fact.Trim());
        }

        AppendIfPresent(builder, "Project ID", input.ProjectId);
        AppendIfPresent(builder, "Project name", input.ProjectName);
        AppendIfPresent(builder, "Requested by", input.RequestedBy);
        AppendIfPresent(builder, "Work scope summary", input.WorkScopeSummary);
        AppendIfPresent(builder, "Proposal summary", input.ProposalSummary);
        AppendIfPresent(builder, "Estimate summary", input.EstimateSummary);

        builder.AppendLine();
        builder.AppendLine("Day-by-day plan");
        if (input.ProposedDailyPlan.Count == 0)
        {
            builder.AppendLine("- No day-by-day plan details were present in the normalized input.");
        }
        else
        {
            foreach (var day in input.ProposedDailyPlan.OrderBy(x => x.Day))
            {
                builder.Append("- Day ").Append(day.Day).Append(": ").AppendLine(day.Activities.Trim());
                if (!string.IsNullOrWhiteSpace(day.Notes))
                {
                    builder.Append("  - Notes: ").AppendLine(day.Notes.Trim());
                }
            }
        }

        builder.AppendLine();
        builder.AppendLine("Assumptions/open items");

        if (string.IsNullOrWhiteSpace(input.ProjectId))
        {
            builder.AppendLine("- Project ID is unknown (mark for confirmation).");
        }

        if (string.IsNullOrWhiteSpace(input.ProjectName))
        {
            builder.AppendLine("- Project name is unknown (mark for confirmation).");
        }

        if (string.IsNullOrWhiteSpace(input.WorkScopeSummary))
        {
            builder.AppendLine("- Scope detail is unknown (proposal/estimate detail required).");
        }

        foreach (var openItem in input.OpenItems.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            builder.Append("- ").AppendLine(openItem.Trim());
        }

        if (input.OpenItems.Count == 0
            && !string.IsNullOrWhiteSpace(input.ProjectId)
            && !string.IsNullOrWhiteSpace(input.ProjectName)
            && !string.IsNullOrWhiteSpace(input.WorkScopeSummary))
        {
            builder.AppendLine("- No unresolved items detected in provided normalized data.");
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendIfPresent(StringBuilder builder, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.Append("- ").Append(label).Append(": ").AppendLine(value.Trim());
        }
    }
}
