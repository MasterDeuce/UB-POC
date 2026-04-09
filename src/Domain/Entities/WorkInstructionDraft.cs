namespace Domain.Entities;

public class WorkInstructionDraft
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkInstructionJobId { get; set; }
    public required string DraftPath { get; set; }
    public string? ContentMarkdown { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
