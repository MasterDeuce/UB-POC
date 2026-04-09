namespace Application.DTOs;

public sealed record ValidateProjectRequest
{
    public required string ProjectId { get; init; }

    public string? ExternalSystemReference { get; init; }
}
