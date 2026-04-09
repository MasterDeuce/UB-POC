namespace Domain.Enums;

public enum WorkInstructionJobStatus
{
    Draft,
    ProjectValidated,
    FilesUploaded,
    Queued,
    Extracting,
    Generating,
    DraftReady,
    DraftEdited,
    Approved,
    Finalizing,
    UploadedToProcore,
    Completed,
    Failed
}
