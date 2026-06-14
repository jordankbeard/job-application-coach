using System.Text;

namespace JobApplicationCoach.Core.Ingest;

public sealed class ChunkingService
{
    private const int ParentContextTokenLimit = 100;

    public IReadOnlyList<DocumentChunk> Chunk(
        IReadOnlyList<ParsedParagraph> paragraphs,
        string sessionId,
        DocumentType documentType)
    {
        var chunks = new List<DocumentChunk>();
        var currentHeading = string.Empty;
        var currentSectionParagraphs = new List<string>();
        var sequenceIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Role == ParagraphRole.SectionHeading)
            {
                currentHeading = paragraph.Content;
                currentSectionParagraphs.Clear();
                continue;
            }

            if (string.IsNullOrWhiteSpace(paragraph.Content))
                continue;

            var parentContext = BuildParentContext(currentHeading, currentSectionParagraphs);

            chunks.Add(new DocumentChunk(
                ChunkId: $"{sessionId}_{documentType}_{sequenceIndex}",
                SessionId: sessionId,
                DocumentType: documentType,
                Content: paragraph.Content,
                SectionHeading: currentHeading,
                ParentContext: parentContext,
                SequenceIndex: sequenceIndex++));

            currentSectionParagraphs.Add(paragraph.Content);
        }

        return chunks.AsReadOnly();
    }

    private static string BuildParentContext(string heading, IList<string> precedingParagraphs)
    {
        var context = new StringBuilder();

        if (!string.IsNullOrEmpty(heading))
            context.AppendLine(heading);

        foreach (var paragraph in precedingParagraphs)
        {
            if (EstimateTokens(context.ToString()) >= ParentContextTokenLimit)
                break;

            context.AppendLine(paragraph);
        }

        return context.ToString().Trim();
    }

    // 1 token ≈ 4 characters for English text — sufficient for capping context size
    private static int EstimateTokens(string text) => text.Length / 4;
}
