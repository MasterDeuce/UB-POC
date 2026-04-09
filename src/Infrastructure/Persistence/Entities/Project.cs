namespace Infrastructure.Persistence.Entities;

public sealed class Project
{
    public Guid Id { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<WorkInstructionJob> WorkInstructionJobs { get; set; } = new List<WorkInstructionJob>();
}
