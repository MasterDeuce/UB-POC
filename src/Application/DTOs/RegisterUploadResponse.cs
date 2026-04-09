namespace Application.DTOs;

public sealed record RegisterUploadResponse
{
    public required string UploadId { get; init; }

    public required string ProjectId { get; init; }

    public required string ExternalDocumentId { get; init; }
}
