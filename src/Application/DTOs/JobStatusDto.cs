using System;

namespace Application.DTOs;

public sealed record JobStatusDto
{
    public required string JobId { get; init; }

    public required string Status { get; init; }

    public string? Message { get; init; }

    public DateTimeOffset LastUpdatedUtc { get; init; }
}
