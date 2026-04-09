namespace Infrastructure.Persistence.Entities;

public sealed class WorkInstructionJob
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string RequestPayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Project Project { get; set; } = null!;
    public ICollection<WorkInstructionExecution> Executions { get; set; } = new List<WorkInstructionExecution>();
}
