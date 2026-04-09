namespace Domain.Entities;

public class IntegrationSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = "Procore";
    public string? ApiBaseUrl { get; set; }
    public string? ClientId { get; set; }
    public string? EncryptedClientSecret { get; set; }
    public string? EncryptedRefreshToken { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}
