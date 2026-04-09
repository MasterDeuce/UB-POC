namespace Domain.Entities;

public class ProjectReference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ProjectNumber { get; set; }
    public required string ProjectName { get; set; }
    public string? ProjectId { get; set; }
    public string? CompanyId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
