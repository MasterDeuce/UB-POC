namespace Infrastructure.Persistence.Entities;

public sealed class WorkInstructionExecution
{
    public Guid Id { get; set; }
    public Guid WorkInstructionJobId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public WorkInstructionJob WorkInstructionJob { get; set; } = null!;
}
