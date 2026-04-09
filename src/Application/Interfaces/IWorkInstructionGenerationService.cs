using System.Threading;
using System.Threading.Tasks;
using Application.Models;

namespace Application.Interfaces;

public interface IWorkInstructionGenerationService
{
    Task<string> GenerateDraftAsync(
        NormalizedWorkInstructionInput normalizedInput,
        string promptTemplateVersion,
        CancellationToken cancellationToken = default);
}
