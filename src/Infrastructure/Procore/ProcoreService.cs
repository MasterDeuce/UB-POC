using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Infrastructure.Procore;

public sealed class ProcoreService : IProcoreService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly ProcoreOptions _options;

    public ProcoreService(HttpClient httpClient, IOptions<ProcoreOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _httpClient.BaseAddress = _options.BaseUrl;
        if (!string.IsNullOrWhiteSpace(_options.BearerToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.BearerToken);
        }
    }

    public async Task<ProjectReference?> ValidateProjectAsync(
        string projectNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectNumber))
        {
            throw new ArgumentException("Project number is required.", nameof(projectNumber));
        }

        if (_options.ProjectMappings.TryGetValue(projectNumber, out var mappedProject))
        {
            var mappedCompanyId = mappedProject.ProcoreCompanyId ?? _options.DefaultCompanyId ?? 0;
            return new ProjectReference(
                mappedProject.ProcoreProjectId,
                mappedCompanyId,
                projectNumber,
                $"Mapped Project {projectNumber}");
        }

        var requestUri = $"rest/v1.0/projects?project_number={Uri.EscapeDataString(projectNumber)}";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var externalProjects = await JsonSerializer.DeserializeAsync<ProcoreProjectResponse[]>(
            stream,
            SerializerOptions,
            cancellationToken);

        var externalProject = externalProjects?.FirstOrDefault();
        return externalProject is null
            ? null
            : ProcoreProjectMapper.ToProjectReference(externalProject, _options.DefaultCompanyId);
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

        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var content = new MultipartFormDataContent();
        content.Add(fileContent, "file", fileName);

        using var response = await _httpClient.PostAsync(
            $"rest/v1.0/projects/{procoreProjectId}/documents",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.TryGetProperty("id", out var idElement))
        {
            return idElement.GetRawText().Trim('"');
        }

        return payload;
    }

    private sealed record ProcoreProjectResponse(
        long Id,
        string Name,
        string ProjectNumber,
        ProcoreCompanyResponse? Company);

    private sealed record ProcoreCompanyResponse(long Id);

    private static class ProcoreProjectMapper
    {
        public static ProjectReference ToProjectReference(
            ProcoreProjectResponse externalProject,
            long? defaultCompanyId)
        {
            var companyId = externalProject.Company?.Id ?? defaultCompanyId ?? 0;

            return new ProjectReference(
                externalProject.Id,
                companyId,
                externalProject.ProjectNumber,
                externalProject.Name);
        }
    }
}
