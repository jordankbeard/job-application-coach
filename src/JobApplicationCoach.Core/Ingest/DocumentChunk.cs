namespace JobApplicationCoach.Core.Ingest;

public sealed record DocumentChunk(
    string ChunkId,
    string SessionId,
    DocumentType DocumentType,
    string Content,
    string SectionHeading,
    string ParentContext,
    int SequenceIndex);
