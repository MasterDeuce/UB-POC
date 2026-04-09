namespace Application.DTOs;

public sealed record SaveDraftEditRequest
{
    public required string JobId { get; init; }

    public required string DraftDocumentPath { get; init; }

    public required string EditedContent { get; init; }

    public required string EditedBy { get; init; }
}
