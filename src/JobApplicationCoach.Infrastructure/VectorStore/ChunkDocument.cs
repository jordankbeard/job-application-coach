using System.Text.Json.Serialization;

namespace JobApplicationCoach.Infrastructure.VectorStore;

internal sealed class ChunkDocument
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; init; } = default!;

    [JsonPropertyName("documentType")]
    public string DocumentType { get; init; } = default!;

    [JsonPropertyName("content")]
    public string Content { get; init; } = default!;

    [JsonPropertyName("sectionHeading")]
    public string SectionHeading { get; init; } = default!;

    [JsonPropertyName("parentContext")]
    public string ParentContext { get; init; } = default!;

    [JsonPropertyName("sequenceIndex")]
    public int SequenceIndex { get; init; }

    [JsonPropertyName("contentVector")]
    public IReadOnlyList<float> ContentVector { get; init; } = default!;
}
