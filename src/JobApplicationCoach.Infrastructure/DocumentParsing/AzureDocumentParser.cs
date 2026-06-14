using System.Text;
using Azure;
using Azure.AI.DocumentIntelligence;
using JobApplicationCoach.Core.Ingest;
using AzureParagraphRole = Azure.AI.DocumentIntelligence.ParagraphRole;

namespace JobApplicationCoach.Infrastructure.DocumentParsing;

public sealed class AzureDocumentParser : IDocumentParser
{
    private const string LayoutModel = "prebuilt-layout";

    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentParser(DocumentIntelligenceClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<ParsedParagraph>> ParseAsync(
        IngestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (IsPlainText(request.FileName))
            return ParsePlainText(request.Content);

        return await ParsePdfAsync(request.Content, cancellationToken);
    }

    private async Task<IReadOnlyList<ParsedParagraph>> ParsePdfAsync(
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        var options = new AnalyzeDocumentOptions(
            LayoutModel,
            BinaryData.FromBytes(content));

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            options,
            cancellationToken);

        return operation.Value.Paragraphs
            .Select((p, index) => new ParsedParagraph(
                Content: p.Content,
                Role: MapRole(p.Role),
                SequenceIndex: index))
            .ToList()
            .AsReadOnly();
    }

    private static IReadOnlyList<ParsedParagraph> ParsePlainText(ReadOnlyMemory<byte> content)
    {
        var text = Encoding.UTF8.GetString(content.Span);

        return text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((line, index) => new ParsedParagraph(
                Content: line,
                Role: Core.Ingest.ParagraphRole.Body,
                SequenceIndex: index))
            .ToList()
            .AsReadOnly();
    }

    // Title is treated as a section heading — both signal structural document boundaries
    private static Core.Ingest.ParagraphRole MapRole(AzureParagraphRole? role)
    {
        if (role == AzureParagraphRole.SectionHeading) return Core.Ingest.ParagraphRole.SectionHeading;
        if (role == AzureParagraphRole.Title)          return Core.Ingest.ParagraphRole.SectionHeading;
        return Core.Ingest.ParagraphRole.Body;
    }

    private static bool IsPlainText(string fileName)
        => Path.GetExtension(fileName).Equals(".txt", StringComparison.OrdinalIgnoreCase);
}
