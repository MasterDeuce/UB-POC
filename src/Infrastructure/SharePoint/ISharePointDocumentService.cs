namespace Infrastructure.SharePoint;

public interface ISharePointDocumentService
{
    Task EnsureProjectFolderAsync(string projectNumber, CancellationToken cancellationToken = default);

    Task<SharePointDocumentReference> UploadEstimateAsync(
        string projectNumber,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<SharePointDocumentReference> UploadProposalAsync(
        string projectNumber,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream> ReadByPathAsync(string canonicalPath, CancellationToken cancellationToken = default);

    Task<Stream> ReadByItemIdAsync(string itemId, CancellationToken cancellationToken = default);

    Task<SharePointDocumentReference> SaveDraftAsync(
        string projectNumber,
        Stream content,
        string? fileName = null,
        CancellationToken cancellationToken = default);

    Task<SharePointDocumentReference> SaveFinalAsync(
        string projectNumber,
        Stream content,
        string? fileName = null,
        CancellationToken cancellationToken = default);
}

public sealed record SharePointDocumentReference(string CanonicalPath, string DriveItemId);
