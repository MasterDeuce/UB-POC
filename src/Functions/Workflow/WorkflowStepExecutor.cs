using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Application.Interfaces;
using Application.Models;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Functions.Workflow;

public sealed partial class WorkflowStepExecutor(
    AppDbContext dbContext,
    IWorkInstructionGenerationService workInstructionGenerationService) : IWorkflowStepExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task ExecuteAsync(Guid jobId, WorkflowStep step, CancellationToken cancellationToken)
    {
        var job = await dbContext.WorkInstructionJobs
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

        if (job is null)
        {
            throw new InvalidOperationException($"Job '{jobId}' was not found.");
        }

        var startedAtUtc = DateTime.UtcNow;
        var payload = ParsePayload(job.RequestPayloadJson);
        var artifacts = GetOrCreateObject(payload, "artifacts");

        _ = step switch
        {
            WorkflowStep.ExtractEstimate => ExecuteExtractEstimate(payload, artifacts),
            WorkflowStep.ExtractProposal => ExecuteExtractProposal(payload, artifacts),
            WorkflowStep.Normalize => ExecuteNormalize(payload, artifacts),
            WorkflowStep.GenerateDraft => await ExecuteGenerateDraftAsync(payload, artifacts, cancellationToken),
            WorkflowStep.SaveDraftToSharePoint => "SaveDraftToSharePoint placeholder completed.",
            WorkflowStep.UpdateStatusAudit => "UpdateStatusAudit placeholder completed.",
            WorkflowStep.GenerateFinalDocx => "GenerateFinalDocx placeholder completed.",
            WorkflowStep.UploadFinalToSharePoint => "UploadFinalToSharePoint placeholder completed.",
            WorkflowStep.UploadToProcore => "UploadToProcore placeholder completed.",
            WorkflowStep.SetCompleted => "Workflow marked as completed.",
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
        };

        MarkStepStatus(payload, step, "Completed", startedAtUtc, DateTime.UtcNow, null);
        job.RequestPayloadJson = payload.ToJsonString(JsonOptions);
        job.UpdatedAtUtc = DateTime.UtcNow;
        job.Status = step is WorkflowStep.SetCompleted ? "Completed" : $"{step}Completed";

        dbContext.WorkInstructionExecutions.Add(new WorkInstructionExecution
        {
            Id = Guid.NewGuid(),
            WorkInstructionJobId = job.Id,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = DateTime.UtcNow,
            Outcome = "Completed",
            ErrorMessage = BuildExecutionDetails(step, "Completed")
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ExecuteExtractEstimate(JsonObject payload, JsonObject artifacts)
    {
        var sourcePath = ResolveDocumentPath(payload, "estimateDocumentPath", "estimateFilePath", "estimatePath");
        var extracted = ExtractEstimateData(sourcePath);
        artifacts["extractedEstimate"] = JsonSerializer.SerializeToNode(extracted, JsonOptions);

        return $"Estimate extracted from '{sourcePath}'.";
    }

    private static string ExecuteExtractProposal(JsonObject payload, JsonObject artifacts)
    {
        var sourcePath = ResolveDocumentPath(payload, "proposalDocumentPath", "proposalFilePath", "proposalPath");
        var extracted = ExtractProposalData(sourcePath);
        artifacts["extractedProposal"] = JsonSerializer.SerializeToNode(extracted, JsonOptions);

        return $"Proposal extracted from '{sourcePath}'.";
    }

    private static string ExecuteNormalize(JsonObject payload, JsonObject artifacts)
    {
        var extractedEstimate = artifacts["extractedEstimate"]?.Deserialize<ExtractedEstimateArtifact>(JsonOptions)
            ?? throw new InvalidOperationException("Estimate extraction artifact is missing. Run ExtractEstimate first.");
        var extractedProposal = artifacts["extractedProposal"]?.Deserialize<ExtractedProposalArtifact>(JsonOptions)
            ?? throw new InvalidOperationException("Proposal extraction artifact is missing. Run ExtractProposal first.");

        var normalized = BuildNormalizedInput(payload, extractedEstimate, extractedProposal);
        artifacts["normalizedInput"] = JsonSerializer.SerializeToNode(normalized, JsonOptions);

        return "Normalized input payload generated.";
    }

    private async Task<string> ExecuteGenerateDraftAsync(
        JsonObject payload,
        JsonObject artifacts,
        CancellationToken cancellationToken)
    {
        var normalized = artifacts["normalizedInput"]?.Deserialize<NormalizedWorkInstructionInput>(JsonOptions)
            ?? throw new InvalidOperationException("Normalized artifact is missing. Run Normalize first.");

        var promptTemplateVersion = payload["promptTemplateVersion"]?.GetValue<string>() ?? "v1";
        var generatedDraft = await workInstructionGenerationService.GenerateDraftAsync(
            normalized,
            promptTemplateVersion,
            cancellationToken);

        artifacts["generatedDraft"] = generatedDraft;

        return "Draft generated successfully.";
    }

    private static NormalizedWorkInstructionInput BuildNormalizedInput(
        JsonObject payload,
        ExtractedEstimateArtifact estimate,
        ExtractedProposalArtifact proposal)
    {
        var estimateSummary = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(estimate.EstimatorName))
        {
            estimateSummary.Append($"Estimator: {estimate.EstimatorName}. ");
        }

        if (estimate.TotalAmount is not null)
        {
            estimateSummary.Append(
                $"Total estimate: {estimate.TotalAmount.Value.ToString("N2", CultureInfo.InvariantCulture)} {estimate.Currency ?? "USD"}.");
        }

        var confirmedFacts = new List<string>();
        confirmedFacts.AddIfPresent($"Estimator name: {estimate.EstimatorName}");
        confirmedFacts.AddIfPresent($"Proposal title: {proposal.ProposalTitle}");
        confirmedFacts.AddIfPresent($"Estimate total: {estimate.TotalAmount?.ToString("N2", CultureInfo.InvariantCulture)} {estimate.Currency}");

        return new NormalizedWorkInstructionInput
        {
            ProjectId = ReadPayloadValue(payload, "projectId"),
            ProjectName = ReadPayloadValue(payload, "projectName") ?? ReadPayloadValue(payload, "projectNumber"),
            RequestedBy = ReadPayloadValue(payload, "requestedBy"),
            WorkScopeSummary = proposal.ScopeSummary,
            ProposalSummary = proposal.ScopeSummary,
            EstimateSummary = estimateSummary.Length == 0 ? null : estimateSummary.ToString().Trim(),
            ProposedDailyPlan = [],
            ConfirmedFacts = confirmedFacts,
            OpenItems = BuildOpenItems(payload, estimate, proposal)
        };
    }

    private static IReadOnlyList<string> BuildOpenItems(
        JsonObject payload,
        ExtractedEstimateArtifact estimate,
        ExtractedProposalArtifact proposal)
    {
        var openItems = new List<string>();

        if (string.IsNullOrWhiteSpace(ReadPayloadValue(payload, "projectId")))
        {
            openItems.Add("ProjectId was not supplied in request payload.");
        }

        if (string.IsNullOrWhiteSpace(proposal.ScopeSummary))
        {
            openItems.Add("Proposal scope summary could not be inferred from the PDF content.");
        }

        if (estimate.TotalAmount is null)
        {
            openItems.Add("Estimate total amount was not detected from the Excel content.");
        }

        return openItems;
    }

    private static ExtractedEstimateArtifact ExtractEstimateData(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Estimate source '{sourcePath}' does not exist.");
        }

        var extension = Path.GetExtension(sourcePath);
        if (string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return ExtractEstimateFromText(File.ReadAllText(sourcePath));
        }

        if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            using var archive = ZipFile.OpenRead(sourcePath);
            var textBuilder = new StringBuilder();
            foreach (var entry in archive.Entries.Where(e => e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                textBuilder.Append(' ').Append(reader.ReadToEnd());
            }

            return ExtractEstimateFromText(textBuilder.ToString());
        }

        return ExtractEstimateFromText(File.ReadAllText(sourcePath));
    }

    private static ExtractedEstimateArtifact ExtractEstimateFromText(string content)
    {
        var estimatorName = MatchNamedValue(content, "estimator", "prepared by", "author");
        var totalAmount = MatchDecimal(content, "total", "total amount", "estimate total", "grand total");
        var currency = MatchCurrency(content);

        var values = ExtractKeyValueLikePairs(content);

        return new ExtractedEstimateArtifact
        {
            EstimatorName = estimatorName,
            TotalAmount = totalAmount,
            Currency = currency,
            Values = values,
            ExtractedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static ExtractedProposalArtifact ExtractProposalData(string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Proposal source '{sourcePath}' does not exist.");
        }

        var content = string.Equals(Path.GetExtension(sourcePath), ".pdf", StringComparison.OrdinalIgnoreCase)
            ? ExtractLikelyPdfText(sourcePath)
            : File.ReadAllText(sourcePath);

        var proposalTitle = MatchNamedValue(content, "title", "proposal", "project");
        var scopeSummary = MatchParagraph(content, "scope", "summary", "work scope");

        return new ExtractedProposalArtifact
        {
            ProposalTitle = proposalTitle,
            ScopeSummary = scopeSummary,
            Values = ExtractKeyValueLikePairs(content),
            ExtractedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static string ExtractLikelyPdfText(string sourcePath)
    {
        var bytes = File.ReadAllBytes(sourcePath);
        var text = Encoding.UTF8.GetString(bytes);

        var tokenMatches = PdfTextRegex().Matches(text)
            .Select(m => m.Groups[1].Value.Replace("\\)", ")", StringComparison.Ordinal))
            .Where(value => !string.IsNullOrWhiteSpace(value));

        var extracted = string.Join(' ', tokenMatches);
        if (!string.IsNullOrWhiteSpace(extracted))
        {
            return extracted;
        }

        return PrintableChunkRegex().Matches(text)
            .Select(m => m.Value)
            .Aggregate(new StringBuilder(), (builder, chunk) => builder.Append(' ').Append(chunk))
            .ToString();
    }

    private static Dictionary<string, string> ExtractKeyValueLikePairs(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex >= trimmed.Length - 1)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            if (key.Length > 0 && value.Length > 0)
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static string? MatchNamedValue(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            var regex = new Regex($"{Regex.Escape(keyword)}\\s*[:=-]\\s*(?<value>[^\\r\\n]+)", RegexOptions.IgnoreCase);
            var match = regex.Match(text);
            if (match.Success)
            {
                return match.Groups["value"].Value.Trim();
            }
        }

        return null;
    }

    private static string? MatchParagraph(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            var regex = new Regex($"{Regex.Escape(keyword)}[^\\r\\n]*[:\\-]\\s*(?<value>[^\\r\\n]{{20,400}})", RegexOptions.IgnoreCase);
            var match = regex.Match(text);
            if (match.Success)
            {
                return match.Groups["value"].Value.Trim();
            }
        }

        return null;
    }

    private static decimal? MatchDecimal(string text, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            var regex = new Regex($"{Regex.Escape(keyword)}[^0-9]*(?<value>[0-9][0-9,]*(?:\\.[0-9]{{1,2}})?)", RegexOptions.IgnoreCase);
            var match = regex.Match(text);
            if (match.Success
                && decimal.TryParse(
                    match.Groups["value"].Value,
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out var decimalValue))
            {
                return decimalValue;
            }
        }

        return null;
    }

    private static string? MatchCurrency(string text)
    {
        if (Regex.IsMatch(text, "\\bUSD\\b|\\$", RegexOptions.IgnoreCase))
        {
            return "USD";
        }

        if (Regex.IsMatch(text, "\\bCAD\\b", RegexOptions.IgnoreCase))
        {
            return "CAD";
        }

        return null;
    }

    private static string ResolveDocumentPath(JsonObject payload, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = ReadPayloadValue(payload, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"No source document path found in payload. Looked for: {string.Join(", ", keys)}.");
    }

    private static string? ReadPayloadValue(JsonObject payload, string key)
    {
        if (payload.TryGetPropertyValue(key, out var valueNode))
        {
            return valueNode?.GetValue<string>();
        }

        return null;
    }

    private static JsonObject ParsePayload(string requestPayloadJson)
    {
        if (string.IsNullOrWhiteSpace(requestPayloadJson))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(requestPayloadJson)?.AsObject() ?? new JsonObject();
        }
        catch (JsonException)
        {
            return new JsonObject
            {
                ["rawPayload"] = requestPayloadJson
            };
        }
    }

    private static JsonObject GetOrCreateObject(JsonObject parent, string propertyName)
    {
        if (parent[propertyName] is JsonObject jsonObject)
        {
            return jsonObject;
        }

        var newObject = new JsonObject();
        parent[propertyName] = newObject;
        return newObject;
    }

    private static void MarkStepStatus(
        JsonObject payload,
        WorkflowStep step,
        string status,
        DateTime startedAtUtc,
        DateTime completedAtUtc,
        string? error)
    {
        var stepStatuses = GetOrCreateObject(payload, "stepStatuses");
        stepStatuses[step.ToString()] = new JsonObject
        {
            ["status"] = status,
            ["startedAtUtc"] = startedAtUtc,
            ["completedAtUtc"] = completedAtUtc,
            ["error"] = error
        };
    }

    private static string BuildExecutionDetails(WorkflowStep step, string status)
    {
        var message = $"Step={step}; Status={status}";
        return message.Length > 1000 ? message[..1000] : message;
    }

    [GeneratedRegex(@"\(([^\)\r\n]{3,200})\)")]
    private static partial Regex PdfTextRegex();

    [GeneratedRegex(@"[A-Za-z0-9][A-Za-z0-9\s,.:;_\-\/]{15,}")]
    private static partial Regex PrintableChunkRegex();

    private sealed record ExtractedEstimateArtifact
    {
        public string? EstimatorName { get; init; }
        public decimal? TotalAmount { get; init; }
        public string? Currency { get; init; }
        public Dictionary<string, string> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public DateTimeOffset ExtractedAtUtc { get; init; }
    }

    private sealed record ExtractedProposalArtifact
    {
        public string? ProposalTitle { get; init; }
        public string? ScopeSummary { get; init; }
        public Dictionary<string, string> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public DateTimeOffset ExtractedAtUtc { get; init; }
    }
}

file static class ListExtensions
{
    public static void AddIfPresent(this List<string> list, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            list.Add(value.Trim());
        }
    }
}
