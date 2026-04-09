namespace Domain.Entities;

public class ExtractedEstimateData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkInstructionJobId { get; set; }
    public string? EstimatorName { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Currency { get; set; }
    public Dictionary<string, string> Values { get; set; } = new();
    public DateTimeOffset ExtractedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
