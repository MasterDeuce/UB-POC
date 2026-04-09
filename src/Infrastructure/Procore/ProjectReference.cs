namespace Infrastructure.Procore;

public sealed record ProjectReference(
    long ProcoreProjectId,
    long ProcoreCompanyId,
    string ProjectNumber,
    string ProjectName);
