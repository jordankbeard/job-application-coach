using JobApplicationCoach.Core.Ingest;
using JobApplicationCoach.Functions.Activities;
using JobApplicationCoach.Functions.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JobApplicationCoach.Functions.Tests.Activities;

public sealed class IngestDocumentActivityTests
{
    private readonly IDocumentParser _parser = Substitute.For<IDocumentParser>();
    private readonly IChunkStore _chunkStore = Substitute.For<IChunkStore>();
    private readonly ChunkingService _chunkingService = new();
    private readonly ILogger<IngestDocumentActivity> _logger = Substitute.For<ILogger<IngestDocumentActivity>>();
    private readonly IngestDocumentActivity _sut;

    public IngestDocumentActivityTests()
    {
        _sut = new IngestDocumentActivity(_parser, _chunkingService, _chunkStore, _logger);
    }

    [Fact]
    public async Task Run_ParsesDocument_WithCvDocumentType()
    {
        _parser.ParseAsync(Arg.Any<IngestRequest>(), Arg.Any<CancellationToken>())
               .Returns([]);

        await _sut.Run(BuildInput("Cv"), CancellationToken.None);

        await _parser.Received(1).ParseAsync(
            Arg.Is<IngestRequest>(r => r.DocumentType == DocumentType.Cv),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_ParsesDocument_WithJobDescriptionDocumentType()
    {
        _parser.ParseAsync(Arg.Any<IngestRequest>(), Arg.Any<CancellationToken>())
               .Returns([]);

        await _sut.Run(BuildInput("JobDescription"), CancellationToken.None);

        await _parser.Received(1).ParseAsync(
            Arg.Is<IngestRequest>(r => r.DocumentType == DocumentType.JobDescription),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_PassesSessionId_ToParser()
    {
        _parser.ParseAsync(Arg.Any<IngestRequest>(), Arg.Any<CancellationToken>())
               .Returns([]);

        await _sut.Run(BuildInput("Cv", sessionId: "my-session-99"), CancellationToken.None);

        await _parser.Received(1).ParseAsync(
            Arg.Is<IngestRequest>(r => r.SessionId == "my-session-99"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_StoresChunks_ProducedByChunkingService()
    {
        var paragraphs = new[]
        {
            new ParsedParagraph("Experience", ParagraphRole.SectionHeading, 0),
            new ParsedParagraph("Led a team of engineers", ParagraphRole.Body, 1)
        };

        _parser.ParseAsync(Arg.Any<IngestRequest>(), Arg.Any<CancellationToken>())
               .Returns(paragraphs);

        await _sut.Run(BuildInput("Cv"), CancellationToken.None);

        await _chunkStore.Received(1).StoreAsync(
            Arg.Is<IReadOnlyList<DocumentChunk>>(chunks => chunks.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_StoresEmptyList_WhenParserReturnsNoParagraphs()
    {
        _parser.ParseAsync(Arg.Any<IngestRequest>(), Arg.Any<CancellationToken>())
               .Returns([]);

        await _sut.Run(BuildInput("Cv"), CancellationToken.None);

        await _chunkStore.Received(1).StoreAsync(
            Arg.Is<IReadOnlyList<DocumentChunk>>(chunks => chunks.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_CallsParser_ExactlyOnce()
    {
        _parser.ParseAsync(Arg.Any<IngestRequest>(), Arg.Any<CancellationToken>())
               .Returns([]);

        await _sut.Run(BuildInput("Cv"), CancellationToken.None);

        await _parser.Received(1).ParseAsync(Arg.Any<IngestRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_Throws_WhenDocumentTypeIsInvalid()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Run(BuildInput("NotAType"), CancellationToken.None));
    }

    private static IngestActivityInput BuildInput(
        string documentType,
        string sessionId = "session-001",
        string fileName = "document.txt") =>
        new(
            SessionId: sessionId,
            DocumentType: documentType,
            Content: "sample content"u8.ToArray(),
            FileName: fileName);
}
