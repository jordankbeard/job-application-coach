namespace JobApplicationCoach.Core.Ingest;

public interface IDocumentParser
{
    Task<IReadOnlyList<ParsedParagraph>> ParseAsync(
        IngestRequest request,
        CancellationToken cancellationToken = default);
}
