using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces;

public interface IJobOrchestrationService
{
    Task<string> StartGenerationAsync(StartGenerationRequest request, CancellationToken cancellationToken = default);

    Task<JobStatusDto> GetJobStatusAsync(string jobId, CancellationToken cancellationToken = default);
}
