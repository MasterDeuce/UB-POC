namespace Application.DTOs;

public sealed record ValidateProjectResponse
{
    public required string ProjectId { get; init; }

    public bool IsValid { get; init; }

    public string? Message { get; init; }
}
