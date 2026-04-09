namespace Application.DTOs;

public sealed record StartGenerationRequest
{
    public required string ProjectId { get; init; }

    public required string EstimateDocumentPath { get; init; }

    public required string ProposalDocumentPath { get; init; }

    public string? RequestedBy { get; init; }
}
