namespace Application.DTOs;

public sealed record RegisterUploadRequest
{
    public required string JobId { get; init; }

    public required string ProjectId { get; init; }

    public required string FileName { get; init; }

    public required string SharePointPath { get; init; }
}
