using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IProposalExtractionService
{
    Task<string> ExtractAsync(string sourceDocumentPath, CancellationToken cancellationToken = default);
}
