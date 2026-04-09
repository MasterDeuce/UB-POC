using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Procore;

public interface IProcoreService
{
    Task<ProjectReference?> ValidateProjectAsync(string projectNumber, CancellationToken cancellationToken = default);

    Task<string> UploadFinalDocumentAsync(
        long procoreProjectId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
