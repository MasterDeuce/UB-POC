using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Functions.Workflow;

public sealed class WorkflowStepExecutor(AppDbContext dbContext) : IWorkflowStepExecutor
{
    public async Task ExecuteAsync(Guid jobId, WorkflowStep step, CancellationToken cancellationToken)
    {
        var job = await dbContext.WorkInstructionJobs
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

        if (job is null)
        {
            throw new InvalidOperationException($"Job '{jobId}' was not found.");
        }

        // Placeholder execution implementation. External integrations should be called here.
        _ = step switch
        {
            WorkflowStep.ExtractEstimate => job.ProjectNumber,
            WorkflowStep.ExtractProposal => job.ProjectNumber,
            WorkflowStep.Normalize => job.RequestPayloadJson,
            WorkflowStep.GenerateDraft => job.RequestPayloadJson,
            WorkflowStep.SaveDraftToSharePoint => job.ProjectNumber,
            WorkflowStep.UpdateStatusAudit => job.Status,
            WorkflowStep.GenerateFinalDocx => job.ProjectNumber,
            WorkflowStep.UploadFinalToSharePoint => job.ProjectNumber,
            WorkflowStep.UploadToProcore => job.ProjectNumber,
            WorkflowStep.SetCompleted => job.ProjectNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
        };

        job.UpdatedAtUtc = DateTime.UtcNow;

        if (step is WorkflowStep.SetCompleted)
        {
            job.Status = "Completed";
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
