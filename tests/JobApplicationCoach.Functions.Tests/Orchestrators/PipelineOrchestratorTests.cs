using JobApplicationCoach.Functions.Activities;
using JobApplicationCoach.Functions.Models;
using JobApplicationCoach.Functions.Orchestrators;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JobApplicationCoach.Functions.Tests.Orchestrators;

public sealed class PipelineOrchestratorTests
{
    private readonly TaskOrchestrationContext _context = Substitute.For<TaskOrchestrationContext>();
    private readonly PipelineOrchestrator _sut = new();

    public PipelineOrchestratorTests()
    {
        _context.CreateReplaySafeLogger<PipelineOrchestrator>()
                .Returns(Substitute.For<ILogger<PipelineOrchestrator>>());
    }

    [Fact]
    public async Task Run_CallsIngestActivity_TwiceTotal()
    {
        await _sut.Run(_context, BuildRequest());

        await _context.Received(2).CallActivityAsync(
            nameof(IngestDocumentActivity),
            Arg.Any<object?>());
    }

    [Fact]
    public async Task Run_CallsIngestActivity_WithCvDocumentType()
    {
        await _sut.Run(_context, BuildRequest());

        await _context.Received(1).CallActivityAsync(
            nameof(IngestDocumentActivity),
            Arg.Is<IngestActivityInput>(i => i.DocumentType == "Cv"));
    }

    [Fact]
    public async Task Run_CallsIngestActivity_WithJobDescriptionDocumentType()
    {
        await _sut.Run(_context, BuildRequest());

        await _context.Received(1).CallActivityAsync(
            nameof(IngestDocumentActivity),
            Arg.Is<IngestActivityInput>(i => i.DocumentType == "JobDescription"));
    }

    [Fact]
    public async Task Run_PassesSessionId_ToBothActivities()
    {
        var request = BuildRequest(sessionId: "abc-123");

        await _sut.Run(_context, request);

        await _context.Received(2).CallActivityAsync(
            nameof(IngestDocumentActivity),
            Arg.Is<IngestActivityInput>(i => i.SessionId == "abc-123"));
    }

    [Fact]
    public async Task Run_PassesCvContent_ToIngestActivity()
    {
        var cvBytes = "cv content"u8.ToArray();
        var request = BuildRequest(cvContent: cvBytes);

        await _sut.Run(_context, request);

        await _context.Received(1).CallActivityAsync(
            nameof(IngestDocumentActivity),
            Arg.Is<IngestActivityInput>(i =>
                i.DocumentType == "Cv" &&
                i.Content.SequenceEqual(cvBytes)));
    }

    private static PipelineRequest BuildRequest(
        string sessionId = "session-001",
        byte[]? cvContent = null,
        byte[]? jdContent = null) =>
        new(
            SessionId: sessionId,
            CvContent: cvContent ?? "cv"u8.ToArray(),
            CvFileName: "cv.txt",
            JdContent: jdContent ?? "jd"u8.ToArray(),
            JdFileName: "jd.txt");
}
