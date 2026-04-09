using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IEstimateExtractionService
{
    Task<string> ExtractAsync(string sourceDocumentPath, CancellationToken cancellationToken = default);
}
