using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface INormalizationService
{
    Task<string> NormalizeAsync(string extractedPayload, CancellationToken cancellationToken = default);
}
