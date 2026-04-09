using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces;

public interface IWorkInstructionGenerationService
{
    Task<string> GenerateDraftAsync(StartGenerationRequest request, CancellationToken cancellationToken = default);
}
