using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Infrastructure.SharePoint;

public sealed class GraphSharePointDocumentService : ISharePointDocumentService
{
    private const string EstimateFileName = "Estimate.pdf";
    private const string ProposalFileName = "Proposal.pdf";
    private const string DefaultDraftFileName = "Draft.docx";
    private const string DefaultFinalFileName = "Final.docx";

    private readonly GraphServiceClient _graphClient;
    private readonly ISharePointDocumentRecordStore _recordStore;
    private readonly ILogger<GraphSharePointDocumentService> _logger;
    private readonly SharePointOptions _options;

    public GraphSharePointDocumentService(
        GraphServiceClient graphClient,
        ISharePointDocumentRecordStore recordStore,
        IOptions<SharePointOptions> options,
        ILogger<GraphSharePointDocumentService> logger)
    {
        _graphClient = graphClient;
        _recordStore = recordStore;
        _logger = logger;
        _options = options.Value;

        if (!_options.IsValid())
        {
            throw new InvalidOperationException(
                $"Invalid {SharePointOptions.SectionName} configuration. TenantId/SiteId/DriveId/LibraryRoot/WorkInstructionsFolder are all required.");
        }
    }

    public async Task EnsureProjectFolderAsync(string projectNumber, CancellationToken cancellationToken = default)
    {
        var projectFolderPath = GetProjectFolderPath(projectNumber);

        _ = await UpsertFolderAsync(projectFolderPath, cancellationToken);
    }

    public Task<SharePointDocumentReference> UploadEstimateAsync(
        string projectNumber,
        Stream content,
        CancellationToken cancellationToken = default) =>
        UploadDocumentAsync(projectNumber, "Estimate", EstimateFileName, content, cancellationToken);

    public Task<SharePointDocumentReference> UploadProposalAsync(
        string projectNumber,
        Stream content,
        CancellationToken cancellationToken = default) =>
        UploadDocumentAsync(projectNumber, "Proposal", ProposalFileName, content, cancellationToken);

    public async Task<Stream> ReadByPathAsync(string canonicalPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalPath);

        _logger.LogInformation("Reading SharePoint file by path: {CanonicalPath}", canonicalPath);

        return await RetryTransientAsync(
            operationName: "ReadByPath",
            operation: async ct => await _graphClient
                .Drives[_options.DriveId]
                .Root
                .ItemWithPath(canonicalPath)
                .Content
                .GetAsync(cancellationToken: ct) ?? Stream.Null,
            cancellationToken: cancellationToken);
    }

    public async Task<Stream> ReadByItemIdAsync(string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        _logger.LogInformation("Reading SharePoint file by item id: {ItemId}", itemId);

        return await RetryTransientAsync(
            operationName: "ReadByItemId",
            operation: async ct => await _graphClient
                .Drives[_options.DriveId]
                .Items[itemId]
                .Content
                .GetAsync(cancellationToken: ct) ?? Stream.Null,
            cancellationToken: cancellationToken);
    }

    public Task<SharePointDocumentReference> SaveDraftAsync(
        string projectNumber,
        Stream content,
        string? fileName = null,
        CancellationToken cancellationToken = default) =>
        UploadDocumentAsync(projectNumber, "Draft", fileName ?? DefaultDraftFileName, content, cancellationToken);

    public Task<SharePointDocumentReference> SaveFinalAsync(
        string projectNumber,
        Stream content,
        string? fileName = null,
        CancellationToken cancellationToken = default) =>
        UploadDocumentAsync(projectNumber, "Final", fileName ?? DefaultFinalFileName, content, cancellationToken);

    private async Task<SharePointDocumentReference> UploadDocumentAsync(
        string projectNumber,
        string documentType,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        await EnsureProjectFolderAsync(projectNumber, cancellationToken);

        var canonicalPath = CombinePath(GetProjectFolderPath(projectNumber), fileName);

        _logger.LogInformation(
            "Uploading {DocumentType} for project {ProjectNumber} to {CanonicalPath}",
            documentType,
            projectNumber,
            canonicalPath);

        var driveItem = await RetryTransientAsync(
            operationName: $"Upload-{documentType}",
            operation: async ct => await _graphClient
                .Drives[_options.DriveId]
                .Root
                .ItemWithPath(canonicalPath)
                .Content
                .PutAsync(content, cancellationToken: ct),
            cancellationToken: cancellationToken);

        if (driveItem?.Id is null)
        {
            throw new InvalidOperationException($"Upload returned no drive item id for path '{canonicalPath}'.");
        }

        await _recordStore.UpsertAsync(
            new SharePointDocumentRecord(
                projectNumber,
                documentType,
                canonicalPath,
                driveItem.Id,
                DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation(
            "Uploaded {DocumentType} for project {ProjectNumber}. Path={CanonicalPath}, ItemId={DriveItemId}",
            documentType,
            projectNumber,
            canonicalPath,
            driveItem.Id);

        return new SharePointDocumentReference(canonicalPath, driveItem.Id);
    }

    private async Task<DriveItem> UpsertFolderAsync(string projectFolderPath, CancellationToken cancellationToken)
    {
        var parentPath = CombinePath(_options.LibraryRoot, _options.WorkInstructionsFolder);
        var folderName = Path.GetFileName(projectFolderPath.TrimEnd('/'));

        return await RetryTransientAsync(
            operationName: "EnsureProjectFolder",
            operation: async ct =>
            {
                try
                {
                    var existing = await _graphClient
                        .Drives[_options.DriveId]
                        .Root
                        .ItemWithPath(projectFolderPath)
                        .GetAsync(cancellationToken: ct);

                    if (existing is not null)
                    {
                        _logger.LogDebug("Project folder already exists at {ProjectFolderPath}", projectFolderPath);
                        return existing;
                    }
                }
                catch (Exception ex) when (IsNotFound(ex))
                {
                    _logger.LogDebug(ex, "Project folder did not exist at {ProjectFolderPath}; creating it", projectFolderPath);
                }

                var newFolder = new DriveItem
                {
                    Name = folderName,
                    Folder = new Folder(),
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["@microsoft.graph.conflictBehavior"] = "replace"
                    }
                };

                var created = await _graphClient
                    .Drives[_options.DriveId]
                    .Root
                    .ItemWithPath(parentPath)
                    .Children
                    .PostAsync(newFolder, cancellationToken: ct);

                return created ?? throw new InvalidOperationException($"Failed to create folder '{projectFolderPath}'.");
            },
            cancellationToken: cancellationToken);
    }

    private string GetProjectFolderPath(string projectNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectNumber);

        return CombinePath(
            _options.LibraryRoot,
            _options.WorkInstructionsFolder,
            projectNumber.Trim());
    }

    private static string CombinePath(params string[] parts)
    {
        var trimmed = parts
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().Trim('/'));

        return string.Join('/', trimmed);
    }

    private async Task<T> RetryTransientAsync<T>(
        string operationName,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 5;
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Transient Graph failure during {OperationName}. Attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}.",
                    operationName,
                    attempt,
                    maxAttempts,
                    delay);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 8000));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Graph operation {OperationName} failed", operationName);
                throw;
            }
        }
    }

    private static bool IsTransient(Exception exception)
    {
        if (exception is TaskCanceledException or TimeoutException)
        {
            return true;
        }

        return GetStatusCode(exception) is
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout or
            HttpStatusCode.InternalServerError;
    }

    private static bool IsNotFound(Exception exception) => GetStatusCode(exception) == HttpStatusCode.NotFound;

    private static HttpStatusCode? GetStatusCode(Exception exception)
    {
        if (exception is ApiException apiException)
        {
            return (HttpStatusCode?)apiException.ResponseStatusCode;
        }

        return null;
    }
}
