using JobApplicationCoach.Core.Ingest;
using JobApplicationCoach.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace JobApplicationCoach.Functions.Activities;

public sealed class IngestDocumentActivity(
    IDocumentParser documentParser,
    ChunkingService chunkingService,
    IChunkStore chunkStore,
    ILogger<IngestDocumentActivity> logger)
{
    [Function(nameof(IngestDocumentActivity))]
    public async Task Run(
        [ActivityTrigger] IngestActivityInput input,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Ingesting {DocumentType} for session {SessionId}",
            input.DocumentType, input.SessionId);

        var documentType = Enum.Parse<DocumentType>(input.DocumentType);

        var ingestRequest = new IngestRequest(
            SessionId: input.SessionId,
            DocumentType: documentType,
            Content: input.Content,
            FileName: input.FileName);

        var paragraphs = await documentParser.ParseAsync(ingestRequest, cancellationToken);

        var chunks = chunkingService.Chunk(paragraphs, input.SessionId, documentType);

        await chunkStore.StoreAsync(chunks, cancellationToken);

        logger.LogInformation(
            "Stored {ChunkCount} chunks for {DocumentType}, session {SessionId}",
            chunks.Count, input.DocumentType, input.SessionId);
    }
}
