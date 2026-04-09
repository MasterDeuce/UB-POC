using Domain.Enums;

namespace Domain.Entities;

public class WorkInstructionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ProjectNumber { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectId { get; set; }

    public WorkInstructionJobStatus Status { get; set; } = WorkInstructionJobStatus.Draft;
    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    // Deterministic file paths.
    public string EstimateFilePath => $"/WorkInstructions/{ProjectNumber}/Estimate.xlsx";
    public string ProposalFilePath => $"/WorkInstructions/{ProjectNumber}/Proposal.pdf";
    public string DraftWorkInstructionsFilePath => $"/WorkInstructions/{ProjectNumber}/DraftWorkInstructions.docx";
    public string FinalWorkInstructionsFilePath => $"/WorkInstructions/{ProjectNumber}/FinalWorkInstructions.docx";

    public List<UploadedDocument> UploadedDocuments { get; set; } = new();
    public ExtractedEstimateData? ExtractedEstimateData { get; set; }
    public ExtractedProposalData? ExtractedProposalData { get; set; }
    public WorkInstructionDraft? Draft { get; set; }
    public List<WorkInstructionAuditLog> AuditLogs { get; set; } = new();
}
