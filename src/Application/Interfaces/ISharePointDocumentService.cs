using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface ISharePointDocumentService
{
    Task EnsureFolderAsync(string folderPath, CancellationToken cancellationToken = default);

    Task<string> UploadByExactPathAsync(
        string exactPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> ReadByExactPathAsync(string exactPath, CancellationToken cancellationToken = default);

    Task<Stream?> ReadByItemIdAsync(string itemId, CancellationToken cancellationToken = default);
}
