namespace Domain.Entities;

public class ExtractedProposalData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkInstructionJobId { get; set; }
    public string? ProposalTitle { get; set; }
    public string? ScopeSummary { get; set; }
    public Dictionary<string, string> Values { get; set; } = new();
    public DateTimeOffset ExtractedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
