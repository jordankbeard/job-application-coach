namespace JobApplicationCoach.Infrastructure.VectorStore;

public sealed class AzureAISearchOptions
{
    public string Endpoint { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string CvIndexName { get; set; } = default!;
    public string JdIndexName { get; set; } = default!;
}
