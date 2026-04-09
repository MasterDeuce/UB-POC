using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces;

public interface IDocumentFinalizationService
{
    Task SaveDraftEditAsync(SaveDraftEditRequest request, CancellationToken cancellationToken = default);

    Task<RegisterUploadResponse> FinalizeDraftAsync(FinalizeDraftRequest request, CancellationToken cancellationToken = default);
}
