using System.Net;
using JobApplicationCoach.Functions.Models;
using JobApplicationCoach.Functions.Orchestrators;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace JobApplicationCoach.Functions.HttpTriggers;

public sealed class PipelineTrigger(ILogger<PipelineTrigger> logger)
{
    [Function(nameof(PipelineTrigger))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "pipeline")] HttpRequestData req,
        [DurableClient] DurableTaskClient durableClient,
        CancellationToken cancellationToken)
    {
        var contentTypeHeader = req.Headers.TryGetValues("Content-Type", out var ctValues)
            ? ctValues.FirstOrDefault() : null;

        if (contentTypeHeader is null || !contentTypeHeader.Contains("multipart/form-data"))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Content-Type must be multipart/form-data.");
            return bad;
        }

        string? sessionId = null;
        byte[]? cvContent = null; string? cvFileName = null;
        byte[]? jdContent = null; string? jdFileName = null;

        try
        {
            var boundary = ExtractBoundary(contentTypeHeader);
            var reader = new MultipartReader(boundary, req.Body)
            {
                BodyLengthLimit = 50 * 1024 * 1024, // 50 MB per section
            };

            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync(cancellationToken)) is not null)
            {
                if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition))
                    continue;

                var fieldName = HeaderUtilities.RemoveQuotes(disposition.Name).Value;

                if (disposition.IsFileDisposition())
                {
                    var (bytes, fileName) = await ReadFileSectionAsync(section, cancellationToken);
                    if (fieldName == "cv")      { cvContent = bytes; cvFileName = fileName; }
                    else if (fieldName == "jd") { jdContent = bytes; jdFileName = fileName; }
                }
                else if (disposition.IsFormDisposition() && fieldName == "sessionId")
                {
                    using var sr = new StreamReader(section.Body);
                    sessionId = await sr.ReadToEndAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse multipart form");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Invalid multipart form data.");
            return bad;
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("sessionId field is required.");
            return bad;
        }
        if (cvContent is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("cv file is required.");
            return bad;
        }
        if (jdContent is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("jd file is required.");
            return bad;
        }

        var request = new PipelineRequest(
            SessionId: sessionId,
            CvContent: cvContent,
            CvFileName: cvFileName ?? "cv",
            JdContent: jdContent,
            JdFileName: jdFileName ?? "jd");

        var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
            nameof(PipelineOrchestrator),
            request);

        logger.LogInformation("Started pipeline orchestration {InstanceId} for session {SessionId}",
            instanceId, sessionId);

        return durableClient.CreateCheckStatusResponse(req, instanceId);
    }

    private static string ExtractBoundary(string contentType)
    {
        var parsed = MediaTypeHeaderValue.Parse(contentType);
        return HeaderUtilities.RemoveQuotes(parsed.Boundary).Value
            ?? throw new InvalidOperationException("Multipart boundary not found in Content-Type header.");
    }

    private static async Task<(byte[] Bytes, string FileName)> ReadFileSectionAsync(
        MultipartSection section, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await section.Body.CopyToAsync(ms, cancellationToken);
        var disposition = ContentDispositionHeaderValue.Parse(section.ContentDisposition);
        var fileName = HeaderUtilities.RemoveQuotes(disposition.FileName).Value ?? "unknown";
        return (ms.ToArray(), fileName);
    }
}
