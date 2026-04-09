using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces;

public interface IProcoreService
{
    Task<ValidateProjectResponse> ValidateProjectAsync(ValidateProjectRequest request, CancellationToken cancellationToken = default);

    Task<RegisterUploadResponse> UploadFinalDocumentAsync(RegisterUploadRequest request, CancellationToken cancellationToken = default);
}
