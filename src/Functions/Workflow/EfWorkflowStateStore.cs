using Infrastructure.Persistence;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Functions.Workflow;

public sealed class EfWorkflowStateStore(AppDbContext dbContext) : IWorkflowStateStore
{
    public async Task<StepStateResult> GetStepStateAsync(Guid jobId, WorkflowStep step, CancellationToken cancellationToken)
    {
        var state = await FindStateAsync(jobId, step, cancellationToken);
        if (state is null)
        {
            return StepStateResult.Pending;
        }

        return new StepStateResult(ParseStatus(state.Status), state.RetryCount);
    }

    public Task MarkInProgressAsync(Guid jobId, WorkflowStep step, int retryCount, CancellationToken cancellationToken)
        => UpsertAsync(jobId, step, StepExecutionStatus.InProgress, retryCount, null, null, cancellationToken);

    public Task MarkCompletedAsync(Guid jobId, WorkflowStep step, int retryCount, CancellationToken cancellationToken)
        => UpsertAsync(jobId, step, StepExecutionStatus.Completed, retryCount, null, DateTime.UtcNow, cancellationToken);

    public Task MarkFailedAsync(Guid jobId, WorkflowStep step, int retryCount, string error, CancellationToken cancellationToken)
        => UpsertAsync(jobId, step, StepExecutionStatus.Failed, retryCount, error, null, cancellationToken);

    private async Task UpsertAsync(
        Guid jobId,
        WorkflowStep step,
        StepExecutionStatus status,
        int retryCount,
        string? error,
        DateTime? completedAtUtc,
        CancellationToken cancellationToken)
    {
        var state = await FindStateAsync(jobId, step, cancellationToken);
        if (state is null)
        {
            state = new WorkflowStepState
            {
                Id = Guid.NewGuid(),
                WorkInstructionJobId = jobId,
                Workflow = ResolveWorkflow(step),
                StepName = step.ToString(),
                Status = status.ToString(),
                RetryCount = retryCount,
                LastError = error,
                LastAttemptedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = completedAtUtc
            };
            dbContext.WorkflowStepStates.Add(state);
        }
        else
        {
            state.Status = status.ToString();
            state.RetryCount = retryCount;
            state.LastError = error;
            state.LastAttemptedAtUtc = DateTime.UtcNow;
            state.CompletedAtUtc = completedAtUtc;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<WorkflowStepState?> FindStateAsync(Guid jobId, WorkflowStep step, CancellationToken cancellationToken)
    {
        return dbContext.WorkflowStepStates.FirstOrDefaultAsync(
            x => x.WorkInstructionJobId == jobId && x.StepName == step.ToString(),
            cancellationToken);
    }

    private static StepExecutionStatus ParseStatus(string status)
    {
        return Enum.TryParse<StepExecutionStatus>(status, ignoreCase: true, out var value)
            ? value
            : StepExecutionStatus.Pending;
    }

    private static string ResolveWorkflow(WorkflowStep step)
    {
        return step <= WorkflowStep.UpdateStatusAudit
            ? WorkflowNames.Generation
            : WorkflowNames.Finalize;
    }
}
