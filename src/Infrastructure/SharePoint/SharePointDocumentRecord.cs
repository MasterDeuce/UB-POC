namespace Infrastructure.SharePoint;

public sealed record SharePointDocumentRecord(
    string ProjectNumber,
    string DocumentType,
    string CanonicalPath,
    string DriveItemId,
    DateTimeOffset LastSyncedAtUtc);
