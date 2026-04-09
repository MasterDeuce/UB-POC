using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Functions.Workflow;

public sealed class WorkflowFunctions(
    IWorkflowStateStore stateStore,
    IWorkflowStepExecutor stepExecutor,
    IConfiguration configuration,
    ILogger<WorkflowFunctions> logger)
{
    private const int MaxRetries = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyList<WorkflowStep> GenerationSteps =
    [
        WorkflowStep.ExtractEstimate,
        WorkflowStep.ExtractProposal,
        WorkflowStep.Normalize,
        WorkflowStep.GenerateDraft,
        WorkflowStep.SaveDraftToSharePoint,
        WorkflowStep.UpdateStatusAudit
    ];

    private static readonly IReadOnlyList<WorkflowStep> FinalizeSteps =
    [
        WorkflowStep.GenerateFinalDocx,
        WorkflowStep.UploadFinalToSharePoint,
        WorkflowStep.UploadToProcore,
        WorkflowStep.SetCompleted
    ];

    [Function(nameof(StartGenerationAsync))]
    public async Task<HttpResponseData> StartGenerationAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jobs/{jobId:guid}/generate")] HttpRequestData request,
        string jobId,
        CancellationToken cancellationToken)
    {
        var workflowMessage = new WorkflowQueueMessage
        {
            JobId = jobId,
            Workflow = WorkflowNames.Generation,
            Step = GenerationSteps[0],
            RetryCount = 0
        };

        await EnqueueAsync(workflowMessage, cancellationToken);

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            JobId = jobId,
            Workflow = WorkflowNames.Generation,
            Status = "Queued",
            NextStep = workflowMessage.Step.ToString()
        }, cancellationToken);

        return response;
    }

    [Function(nameof(StartFinalizeAsync))]
    public async Task<HttpResponseData> StartFinalizeAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jobs/{jobId:guid}/finalize")] HttpRequestData request,
        string jobId,
        CancellationToken cancellationToken)
    {
        var workflowMessage = new WorkflowQueueMessage
        {
            JobId = jobId,
            Workflow = WorkflowNames.Finalize,
            Step = FinalizeSteps[0],
            RetryCount = 0
        };

        await EnqueueAsync(workflowMessage, cancellationToken);

        var response = request.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            JobId = jobId,
            Workflow = WorkflowNames.Finalize,
            Status = "Queued",
            NextStep = workflowMessage.Step.ToString()
        }, cancellationToken);

        return response;
    }

    [Function(nameof(ProcessWorkflowStepAsync))]
    public async Task ProcessWorkflowStepAsync(
        [QueueTrigger(WorkflowQueueNames.WorkflowQueue, Connection = "AzureWebJobsStorage")] string messagePayload,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<WorkflowQueueMessage>(messagePayload, JsonOptions)
            ?? throw new InvalidOperationException("Invalid queue message payload.");

        if (!Guid.TryParse(message.JobId, out var jobId))
        {
            logger.LogError("Queue message contains invalid JobId '{JobId}'.", message.JobId);
            return;
        }

        var stepState = await stateStore.GetStepStateAsync(jobId, message.Step, cancellationToken);
        if (stepState.Status is StepExecutionStatus.Completed)
        {
            await EnqueueNextStepAsync(message, cancellationToken);
            return;
        }

        try
        {
            await stateStore.MarkInProgressAsync(jobId, message.Step, message.RetryCount, cancellationToken);
            await stepExecutor.ExecuteAsync(jobId, message.Step, cancellationToken);
            await stateStore.MarkCompletedAsync(jobId, message.Step, message.RetryCount, cancellationToken);

            await EnqueueNextStepAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            var retryCount = message.RetryCount + 1;
            await stateStore.MarkFailedAsync(jobId, message.Step, retryCount, ex.Message, cancellationToken);

            if (retryCount > MaxRetries)
            {
                logger.LogError(ex,
                    "Step {Step} for job {JobId} exceeded retry threshold ({RetryCount}/{MaxRetries}).",
                    message.Step,
                    jobId,
                    retryCount,
                    MaxRetries);
                return;
            }

            logger.LogWarning(ex,
                "Retrying step {Step} for job {JobId}. Attempt {RetryCount}/{MaxRetries}.",
                message.Step,
                jobId,
                retryCount,
                MaxRetries);

            await EnqueueAsync(message with { RetryCount = retryCount }, cancellationToken);
        }
    }

    private async Task EnqueueNextStepAsync(WorkflowQueueMessage message, CancellationToken cancellationToken)
    {
        var steps = ResolveSteps(message.Workflow);
        var currentIndex = FindStepIndex(steps, message.Step);
        if (currentIndex < 0 || currentIndex == steps.Count - 1)
        {
            return;
        }

        var nextMessage = message with
        {
            Step = steps[currentIndex + 1],
            RetryCount = 0
        };

        await EnqueueAsync(nextMessage, cancellationToken);
    }

    private async Task EnqueueAsync(WorkflowQueueMessage message, CancellationToken cancellationToken)
    {
        var connectionString = configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("AzureWebJobsStorage was not configured.");

        var queueClient = new QueueClient(connectionString, WorkflowQueueNames.WorkflowQueue);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        await queueClient.SendMessageAsync(payload, cancellationToken);
    }

    private static IReadOnlyList<WorkflowStep> ResolveSteps(string workflow)
    {
        return string.Equals(workflow, WorkflowNames.Finalize, StringComparison.OrdinalIgnoreCase)
            ? FinalizeSteps
            : GenerationSteps;
    }

    private static int FindStepIndex(IReadOnlyList<WorkflowStep> steps, WorkflowStep step)
    {
        for (var index = 0; index < steps.Count; index++)
        {
            if (steps[index] == step)
            {
                return index;
            }
        }

        return -1;
    }
}
