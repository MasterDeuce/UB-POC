namespace Infrastructure.SharePoint;

public interface ISharePointDocumentRecordStore
{
    Task UpsertAsync(SharePointDocumentRecord record, CancellationToken cancellationToken = default);
}
