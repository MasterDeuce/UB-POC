namespace Domain.Enums;

public enum AuditActionType
{
    Created,
    ProjectValidated,
    FileUploaded,
    StatusChanged,
    ExtractionStarted,
    ExtractionCompleted,
    DraftGenerated,
    DraftEdited,
    Approved,
    Finalized,
    UploadedToProcore,
    Completed,
    Failed,
    IntegrationSettingsUpdated,
    CommentAdded
}
