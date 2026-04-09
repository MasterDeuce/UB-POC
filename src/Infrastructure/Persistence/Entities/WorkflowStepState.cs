namespace Infrastructure.Persistence.Entities;

public sealed class WorkflowStepState
{
    public Guid Id { get; set; }
    public Guid WorkInstructionJobId { get; set; }
    public string Workflow { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTime LastAttemptedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public WorkInstructionJob WorkInstructionJob { get; set; } = null!;
}
