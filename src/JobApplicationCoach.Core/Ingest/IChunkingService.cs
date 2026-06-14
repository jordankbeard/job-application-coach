namespace JobApplicationCoach.Core.Ingest;

public interface IChunkingService
{
    IReadOnlyList<DocumentChunk> Chunk(
        IReadOnlyList<ParsedParagraph> paragraphs,
        string sessionId,
        DocumentType documentType);
}
