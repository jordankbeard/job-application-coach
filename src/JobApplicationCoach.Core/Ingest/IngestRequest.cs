namespace JobApplicationCoach.Core.Ingest;

public sealed record IngestRequest(
    string SessionId,
    DocumentType DocumentType,
    ReadOnlyMemory<byte> Content,
    string FileName);

public enum DocumentType
{
    Cv,
    JobDescription
}
