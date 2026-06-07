using JobApplicationCoach.Functions.Activities;
using JobApplicationCoach.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace JobApplicationCoach.Functions.Orchestrators;

public sealed class PipelineOrchestrator
{
    [Function(nameof(PipelineOrchestrator))]
    public async Task Run(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        PipelineRequest request)
    {
        var logger = context.CreateReplaySafeLogger<PipelineOrchestrator>();

        logger.LogInformation("Pipeline started for session {SessionId}", request.SessionId);

        var cvInput = new IngestActivityInput(
            SessionId: request.SessionId,
            DocumentType: "Cv",
            Content: Convert.FromBase64String(request.CvContentBase64),
            FileName: request.CvFileName);

        var jdInput = new IngestActivityInput(
            SessionId: request.SessionId,
            DocumentType: "JobDescription",
            Content: Convert.FromBase64String(request.JdContentBase64),
            FileName: request.JdFileName);

        await context.CallActivityAsync(nameof(IngestDocumentActivity), cvInput);
        await context.CallActivityAsync(nameof(IngestDocumentActivity), jdInput);

        // TODO: AnalyseGapActivity and RewriteBulletsActivity fan-out added in §1.5
        logger.LogInformation("Ingest complete for session {SessionId}", request.SessionId);
    }
}
