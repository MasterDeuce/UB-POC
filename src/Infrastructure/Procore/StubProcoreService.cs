using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Infrastructure.Procore;

public sealed class StubProcoreService : IProcoreService
{
    private readonly ProcoreOptions _options;
    private readonly ConcurrentDictionary<long, byte> _uploadedDocuments = new();

    public StubProcoreService(IOptions<ProcoreOptions> options)
    {
        _options = options.Value;
    }

    public Task<ProjectReference?> ValidateProjectAsync(
        string projectNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectNumber))
        {
            throw new ArgumentException("Project number is required.", nameof(projectNumber));
        }

        if (_options.ProjectMappings.TryGetValue(projectNumber, out var mappedProject))
        {
            var companyId = mappedProject.ProcoreCompanyId ?? _options.DefaultCompanyId ?? 0;
            return Task.FromResult<ProjectReference?>(new ProjectReference(
                mappedProject.ProcoreProjectId,
                companyId,
                projectNumber,
                $"Stubbed Project {projectNumber}"));
        }

        return Task.FromResult<ProjectReference?>(null);
    }

    public async Task<string> UploadFinalDocumentAsync(
        long procoreProjectId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (procoreProjectId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(procoreProjectId));
        }

        if (fileStream is null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        // Consume stream to mimic upload behavior and catch invalid streams during development.
        await fileStream.CopyToAsync(Stream.Null, cancellationToken);
        _uploadedDocuments[procoreProjectId] = 1;

        return $"stub-doc-{procoreProjectId}-{Path.GetFileName(fileName)}";
    }
}
