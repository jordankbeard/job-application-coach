using JobApplicationCoach.Core.Ingest;

namespace JobApplicationCoach.Core.Tests.Ingest;

public sealed class ChunkingServiceTests
{
    private const string SessionId = "session-001";
    private readonly ChunkingService _sut = new();

    [Fact]
    public void Chunk_WithSectionHeading_StampsHeadingOnAllChunksInSection()
    {
        var paragraphs = new[]
        {
            Para("Experience", ParagraphRole.SectionHeading, 0),
            Para("Led a team of 5 engineers",   ParagraphRole.Body, 1),
            Para("Delivered microservices MVP",  ParagraphRole.Body, 2)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        Assert.All(chunks, c => Assert.Equal("Experience", c.SectionHeading));
    }

    [Fact]
    public void Chunk_DoesNotProduceChunkForSectionHeading()
    {
        var paragraphs = new[]
        {
            Para("Skills",              ParagraphRole.SectionHeading, 0),
            Para("C#, Azure, Docker",   ParagraphRole.Body,           1)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        Assert.Single(chunks);
        Assert.Equal("C#, Azure, Docker", chunks[0].Content);
    }

    [Fact]
    public void Chunk_SkipsEmptyAndWhitespaceParagraphs()
    {
        var paragraphs = new[]
        {
            Para("Experience",  ParagraphRole.SectionHeading, 0),
            Para("",            ParagraphRole.Body,           1),
            Para("   ",         ParagraphRole.Body,           2),
            Para("Valid chunk", ParagraphRole.Body,           3)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        Assert.Single(chunks);
    }

    [Fact]
    public void Chunk_ParentContextOfFirstChunkInSection_ContainsOnlyHeading()
    {
        var paragraphs = new[]
        {
            Para("Experience",             ParagraphRole.SectionHeading, 0),
            Para("Led a team of 5 engineers", ParagraphRole.Body,   1)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        Assert.Equal("Experience", chunks[0].ParentContext);
    }

    [Fact]
    public void Chunk_ParentContextOfLaterChunks_AccumulatesPrecedingContent()
    {
        var paragraphs = new[]
        {
            Para("Experience",                  ParagraphRole.SectionHeading, 0),
            Para("Led a team of 5 engineers",   ParagraphRole.Body,       1),
            Para("Delivered microservices MVP", ParagraphRole.Body,       2)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        Assert.Contains("Led a team of 5 engineers", chunks[1].ParentContext);
    }

    [Fact]
    public void Chunk_WhenNewHeadingAppears_ResetsParentContext()
    {
        var paragraphs = new[]
        {
            Para("Experience",              ParagraphRole.SectionHeading, 0),
            Para("Led a team of 5 engineers", ParagraphRole.Body,    1),
            Para("Skills",                  ParagraphRole.SectionHeading, 2),
            Para("C#, Azure",               ParagraphRole.Body,           3)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        var skillsChunk = chunks.Single(c => c.SectionHeading == "Skills");
        Assert.DoesNotContain("Led a team", skillsChunk.ParentContext);
    }

    [Fact]
    public void Chunk_SequenceIndexIsZeroBased_AndIncrementsPerChunk()
    {
        var paragraphs = new[]
        {
            Para("Experience",   ParagraphRole.SectionHeading, 0),
            Para("First bullet", ParagraphRole.Body,       1),
            Para("Second bullet",ParagraphRole.Body,       2),
            Para("Third bullet", ParagraphRole.Body,       3)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        Assert.Equal([0, 1, 2], chunks.Select(c => c.SequenceIndex));
    }

    [Fact]
    public void Chunk_StampsCorrectDocumentType_OnAllChunks()
    {
        var paragraphs = new[]
        {
            Para("Requirements",      ParagraphRole.SectionHeading, 0),
            Para("5+ years .NET exp", ParagraphRole.Body,       1)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.JobDescription);

        Assert.All(chunks, c => Assert.Equal(DocumentType.JobDescription, c.DocumentType));
    }

    [Fact]
    public void Chunk_WithNoHeadings_ProducesChunksWithEmptySectionHeading()
    {
        var paragraphs = new[]
        {
            Para("Some free-form text", ParagraphRole.Body, 0),
            Para("More text",           ParagraphRole.Body, 1)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        Assert.Equal(2, chunks.Count);
        Assert.All(chunks, c => Assert.Equal(string.Empty, c.SectionHeading));
    }

    [Fact]
    public void Chunk_AssignsUniqueChunkIds()
    {
        var paragraphs = new[]
        {
            Para("Experience",   ParagraphRole.SectionHeading, 0),
            Para("First bullet", ParagraphRole.Body,       1),
            Para("Second bullet",ParagraphRole.Body,       2)
        };

        var chunks = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        var ids = chunks.Select(c => c.ChunkId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Chunk_ChunkIdsAreDeterministic_SameInputProducesSameIds()
    {
        var paragraphs = new[]
        {
            Para("Experience",   ParagraphRole.SectionHeading, 0),
            Para("First bullet", ParagraphRole.Body,           1),
        };

        var first  = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);
        var second = _sut.Chunk(paragraphs, SessionId, DocumentType.Cv);

        Assert.Equal(first[0].ChunkId, second[0].ChunkId);
    }

    private static ParsedParagraph Para(string content, ParagraphRole role, int index)
        => new(content, role, index);
}
