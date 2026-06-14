using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using JobApplicationCoach.Core.Ingest;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace JobApplicationCoach.Infrastructure.VectorStore;

public sealed class AzureAISearchService : IChunkStore
{
    private readonly SearchIndexClient _indexClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly AzureAISearchOptions _options;

    public AzureAISearchService(
        SearchIndexClient indexClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<AzureAISearchOptions> options)
    {
        _indexClient = indexClient;
        _embeddingGenerator = embeddingGenerator;
        _options = options.Value;
    }

    public async Task StoreAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0) return;

        var distinctTypes = chunks.Select(c => c.DocumentType).Distinct().ToList();
        if (distinctTypes.Count > 1)
            throw new ArgumentException(
                $"All chunks must share the same DocumentType. Got: {string.Join(", ", distinctTypes)}.",
                nameof(chunks));

        var indexName = ResolveIndexName(chunks[0].DocumentType);
        var searchClient = _indexClient.GetSearchClient(indexName);

        var contents = chunks.Select(c => c.Content).ToList();

        // Batch all embedding calls into one API request — one call for N chunks
        var embeddings = await _embeddingGenerator.GenerateAsync(contents, cancellationToken: cancellationToken);

        var documents = chunks
            .Zip(embeddings, (chunk, embedding) => new ChunkDocument
            {
                Id = chunk.ChunkId,
                SessionId = chunk.SessionId,
                DocumentType = chunk.DocumentType.ToString(),
                Content = chunk.Content,
                SectionHeading = chunk.SectionHeading,
                ParentContext = chunk.ParentContext,
                SequenceIndex = chunk.SequenceIndex,
                ContentVector = embedding.Vector.ToArray()
            })
            .ToList();

        // MergeOrUpload is idempotent — re-ingesting the same session overwrites rather than duplicates
        var batch = IndexDocumentsBatch.MergeOrUpload(documents);
        var result = await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

        var failed = result.Value.Results.Where(r => !r.Succeeded).ToList();
        if (failed.Count > 0)
            throw new InvalidOperationException(
                $"{failed.Count} of {chunks.Count} chunks failed to index for {chunks[0].DocumentType}. " +
                $"Failed chunk keys: {string.Join(", ", failed.Select(f => f.Key))}");
    }

    private string ResolveIndexName(DocumentType documentType) => documentType switch
    {
        DocumentType.Cv => _options.CvIndexName,
        DocumentType.JobDescription => _options.JdIndexName,
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, null)
    };
}
