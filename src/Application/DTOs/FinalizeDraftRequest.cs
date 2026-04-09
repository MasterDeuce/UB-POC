namespace Application.DTOs;

public sealed record FinalizeDraftRequest
{
    public required string JobId { get; init; }

    public required string ProjectId { get; init; }

    public required string DraftDocumentPath { get; init; }

    public required string FinalizedBy { get; init; }
}
