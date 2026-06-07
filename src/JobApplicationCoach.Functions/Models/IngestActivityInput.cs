namespace JobApplicationCoach.Functions.Models;

// Serialisable boundary type — orchestrators can only pass JSON-safe types to activities.
// ReadOnlyMemory<byte> from IngestRequest cannot cross this boundary; byte[] can.
public sealed record IngestActivityInput(
    string SessionId,
    string DocumentType,
    byte[] Content,
    string FileName);
