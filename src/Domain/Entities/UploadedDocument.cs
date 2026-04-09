namespace Domain.Entities;

public class UploadedDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkInstructionJobId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string StoragePath { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
