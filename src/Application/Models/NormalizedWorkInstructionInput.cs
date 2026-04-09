namespace Application.Models;

public sealed record NormalizedWorkInstructionInput
{
    public string? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public string? RequestedBy { get; init; }

    public string? WorkScopeSummary { get; init; }

    public string? ProposalSummary { get; init; }

    public string? EstimateSummary { get; init; }

    public IReadOnlyList<DailyPlanItem> ProposedDailyPlan { get; init; } = [];

    public IReadOnlyList<string> ConfirmedFacts { get; init; } = [];

    public IReadOnlyList<string> OpenItems { get; init; } = [];
}

public sealed record DailyPlanItem
{
    public required int Day { get; init; }

    public required string Activities { get; init; }

    public string? Notes { get; init; }
}
