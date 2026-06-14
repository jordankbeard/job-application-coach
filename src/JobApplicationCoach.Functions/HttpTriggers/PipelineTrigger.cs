using JobApplicationCoach.Functions.Models;
using JobApplicationCoach.Functions.Orchestrators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace JobApplicationCoach.Functions.HttpTriggers;

public sealed class PipelineTrigger(ILogger<PipelineTrigger> logger)
{
    [Function(nameof(PipelineTrigger))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "pipeline")] HttpRequest req,
        [DurableClient] DurableTaskClient durableClient,
        CancellationToken cancellationToken)
    {
        var form = await req.ReadFormAsync(cancellationToken);

        var sessionId = form["sessionId"].FirstOrDefault();
        var cvFile    = form.Files["cv"];
        var jdFile    = form.Files["jd"];

        if (string.IsNullOrWhiteSpace(sessionId))
            return new BadRequestObjectResult("sessionId field is required.");
        if (cvFile is null)
            return new BadRequestObjectResult("cv file is required.");
        if (jdFile is null)
            return new BadRequestObjectResult("jd file is required.");

        var request = new PipelineRequest(
            SessionId: sessionId,
            CvContent: await ReadFileAsync(cvFile, cancellationToken),
            CvFileName: cvFile.FileName,
            JdContent: await ReadFileAsync(jdFile, cancellationToken),
            JdFileName: jdFile.FileName);

        var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(PipelineOrchestrator),
            request,
            cancellation: cancellationToken);

        logger.LogInformation("Started pipeline orchestration {InstanceId} for session {SessionId}",
            instanceId, sessionId);

        var payload = durableClient.CreateHttpManagementPayload(instanceId);
        return new ObjectResult(payload) { StatusCode = StatusCodes.Status202Accepted };
    }

    private static async Task<byte[]> ReadFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}
