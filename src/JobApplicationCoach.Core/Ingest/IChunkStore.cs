namespace JobApplicationCoach.Core.Ingest;

public interface IChunkStore
{
    Task StoreAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default);
}
