using JobApplicationCoach.Core.Ingest;
using JobApplicationCoach.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JobApplicationCoach.Functions.Tests;

/// <summary>
/// Level 1 DI composition tests — verify the container wires up correctly without hitting Azure.
/// Azure SDK clients (DocumentIntelligenceClient, SearchIndexClient) do not connect at construction
/// time, so fake-but-valid-format config values are sufficient.
///
/// TODO Level 2: Add integration tests that hit real Azure test resources to verify end-to-end
/// behaviour (parse → chunk → embed → upsert). Requires dedicated Azure test indexes and
/// credentials in CI secrets. See docs/plan.md for details.
/// </summary>
public sealed class CompositionRootTests
{
    private readonly IServiceProvider _services;

    public CompositionRootTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // No API keys — all Azure services use Entra ID (DefaultAzureCredential).
                // Clients don't connect at construction time so fake endpoints are sufficient
                // for Level 1 DI composition tests.
                //
                // Keys use ":" (colon) as the hierarchy separator — the IConfiguration standard.
                // In local.settings.json / Azure App Settings the same keys use "__" (double
                // underscore) because ":" is not valid in environment variable names.
                // The EnvironmentVariablesConfigurationProvider converts "__" → ":" on load,
                // so the code must always use ":" when reading IConfiguration.
                ["AzureDocumentIntelligence:Endpoint"] = "https://fake.cognitiveservices.azure.com/",
                ["AzureAISearch:Endpoint"]             = "https://fake.search.windows.net",
                ["AzureAISearch:CvIndexName"]          = "cv-chunks",
                ["AzureAISearch:JdIndexName"]          = "jd-chunks",
                ["AzureOpenAI:Endpoint"]               = "https://fake.openai.azure.com/",
                ["AzureOpenAI:ChatDeployment"]         = "gpt-4o",
                ["AzureOpenAI:EmbeddingDeployment"]    = "text-embedding-3-small",
            })
            .Build();

        _services = new ServiceCollection()
            .AddLogging()
            .AddDocumentParsing(configuration)
            .AddSemanticKernel(configuration)
            .AddVectorStore(configuration)
            .AddSingleton<ChunkingService>()
            .BuildServiceProvider();
    }

    [Fact]
    public void IDocumentParser_Resolves_AsAzureDocumentParser()
    {
        var service = _services.GetRequiredService<IDocumentParser>();
        Assert.IsType<Infrastructure.DocumentParsing.AzureDocumentParser>(service);
    }

    [Fact]
    public void IChunkStore_Resolves_AsAzureAISearchService()
    {
        var service = _services.GetRequiredService<IChunkStore>();
        Assert.IsType<Infrastructure.VectorStore.AzureAISearchService>(service);
    }

    [Fact]
    public void ChunkingService_Resolves()
    {
        var service = _services.GetRequiredService<ChunkingService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void IEmbeddingGenerator_Resolves()
    {
        var service = _services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        Assert.NotNull(service);
    }

    [Fact]
    public void IngestDocumentActivity_AllDependencies_Resolve()
    {
        // IngestDocumentActivity is not registered in DI (Functions runtime handles that),
        // so we verify each constructor parameter resolves individually.
        Assert.NotNull(_services.GetRequiredService<IDocumentParser>());
        Assert.NotNull(_services.GetRequiredService<ChunkingService>());
        Assert.NotNull(_services.GetRequiredService<IChunkStore>());
        Assert.NotNull(_services.GetRequiredService<ILogger<Functions.Activities.IngestDocumentActivity>>());
    }
}
