namespace Functions.Workflow;

public static class WorkflowQueueNames
{
    public const string WorkflowQueue = "workinstruction-workflow";
}

public static class WorkflowNames
{
    public const string Generation = "generation";
    public const string Finalize = "finalize";
}

public enum WorkflowStep
{
    ExtractEstimate = 1,
    ExtractProposal = 2,
    Normalize = 3,
    GenerateDraft = 4,
    SaveDraftToSharePoint = 5,
    UpdateStatusAudit = 6,
    GenerateFinalDocx = 7,
    UploadFinalToSharePoint = 8,
    UploadToProcore = 9,
    SetCompleted = 10
}

public enum StepExecutionStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}

public sealed record WorkflowQueueMessage
{
    public required string JobId { get; init; }
    public required string Workflow { get; init; }
    public required WorkflowStep Step { get; init; }
    public int RetryCount { get; init; }
}

public sealed record StepStateResult(StepExecutionStatus Status, int RetryCount)
{
    public static StepStateResult Pending => new(StepExecutionStatus.Pending, 0);
}

public interface IWorkflowStateStore
{
    Task<StepStateResult> GetStepStateAsync(Guid jobId, WorkflowStep step, CancellationToken cancellationToken);
    Task MarkInProgressAsync(Guid jobId, WorkflowStep step, int retryCount, CancellationToken cancellationToken);
    Task MarkCompletedAsync(Guid jobId, WorkflowStep step, int retryCount, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid jobId, WorkflowStep step, int retryCount, string error, CancellationToken cancellationToken);
}

public interface IWorkflowStepExecutor
{
    Task ExecuteAsync(Guid jobId, WorkflowStep step, CancellationToken cancellationToken);
}
