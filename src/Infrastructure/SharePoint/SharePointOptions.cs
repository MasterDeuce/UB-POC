namespace Infrastructure.SharePoint;

public sealed class SharePointOptions
{
    public const string SectionName = "SharePoint";

    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Site id in the form {hostname},{siteCollectionId},{webId}.
    /// </summary>
    public string SiteId { get; init; } = string.Empty;

    public string DriveId { get; init; } = string.Empty;

    /// <summary>
    /// Document library root path if operating by path (for example: Shared Documents).
    /// </summary>
    public string LibraryRoot { get; init; } = "Shared Documents";

    /// <summary>
    /// Root work instructions folder relative to the library root.
    /// </summary>
    public string WorkInstructionsFolder { get; init; } = "WorkInstructions";

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(SiteId) &&
        !string.IsNullOrWhiteSpace(DriveId) &&
        !string.IsNullOrWhiteSpace(LibraryRoot) &&
        !string.IsNullOrWhiteSpace(WorkInstructionsFolder);
}
