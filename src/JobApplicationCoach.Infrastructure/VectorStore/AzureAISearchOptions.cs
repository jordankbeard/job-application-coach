namespace JobApplicationCoach.Infrastructure.VectorStore;

public sealed class AzureAISearchOptions
{
    public string CvIndexName { get; set; } = default!;
    public string JdIndexName { get; set; } = default!;
}
