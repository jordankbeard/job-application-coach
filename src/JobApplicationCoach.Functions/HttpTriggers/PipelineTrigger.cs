using System.Net;
using System.Text.Json;
using JobApplicationCoach.Functions.Models;
using JobApplicationCoach.Functions.Orchestrators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace JobApplicationCoach.Functions.HttpTriggers;

public sealed class PipelineTrigger(ILogger<PipelineTrigger> logger)
{
    [Function(nameof(PipelineTrigger))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "pipeline")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        CancellationToken cancellationToken)
    {
        PipelineRequest? request;

        try
        {
            request = await JsonSerializer.DeserializeAsync<PipelineRequest>(
                req.Body,
                cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid pipeline request body");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid request body.");
            return bad;
        }

        if (request is null || string.IsNullOrWhiteSpace(request.SessionId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("SessionId is required.");
            return bad;
        }

        var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(PipelineOrchestrator),
            request);

        logger.LogInformation("Started pipeline orchestration {InstanceId} for session {SessionId}",
            instanceId, request.SessionId);

        return durableClient.CreateCheckStatusResponse(req, instanceId);
    }
}
