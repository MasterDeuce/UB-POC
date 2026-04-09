using System;
using System.Collections.Generic;

namespace Infrastructure.Procore;

public sealed class ProcoreOptions
{
    public const string SectionName = "Procore";

    public Uri BaseUrl { get; set; } = new("https://api.procore.com/");

    /// <summary>
    /// Bearer token sent to Procore APIs.
    /// </summary>
    public string BearerToken { get; set; } = string.Empty;

    /// <summary>
    /// Fallback company id when response does not include a company block.
    /// </summary>
    public long? DefaultCompanyId { get; set; }

    /// <summary>
    /// Local mapping of internal project numbers to Procore ids.
    /// </summary>
    public Dictionary<string, ProcoreProjectMapping> ProjectMappings { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ProcoreProjectMapping
{
    public long ProcoreProjectId { get; set; }

    public long? ProcoreCompanyId { get; set; }
}
