using Domain.Enums;

namespace Domain.Entities;

public class WorkInstructionAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkInstructionJobId { get; set; }
    public AuditActionType ActionType { get; set; }
    public required string Message { get; set; }
    public string? PerformedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
