using Azure.AI.DocumentIntelligence;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using JobApplicationCoach.Core.Ingest;
using JobApplicationCoach.Infrastructure.DocumentParsing;
using JobApplicationCoach.Infrastructure.Kernel;
using JobApplicationCoach.Infrastructure.VectorStore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SKernel = Microsoft.SemanticKernel.Kernel;

namespace JobApplicationCoach.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddDocumentParsing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TryAddSingleton is idempotent — if AddSemanticKernel or AddVectorStore was called
        // first, the same DefaultAzureCredential instance is reused across all three clients.
        services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

        services.AddSingleton(sp =>
        {
            var endpoint = new Uri(configuration["AzureDocumentIntelligence__Endpoint"]
                ?? throw new InvalidOperationException("AzureDocumentIntelligence__Endpoint is not configured."));

            return new DocumentIntelligenceClient(endpoint, sp.GetRequiredService<TokenCredential>());
        });

        services.AddSingleton<IDocumentParser, AzureDocumentParser>();

        return services;
    }

    public static IServiceCollection AddVectorStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

        services.AddSingleton(sp =>
        {
            var endpoint = new Uri(configuration["AzureAISearch__Endpoint"]
                ?? throw new InvalidOperationException("AzureAISearch__Endpoint is not configured."));

            return new SearchIndexClient(endpoint, sp.GetRequiredService<TokenCredential>());
        });

        services.Configure<AzureAISearchOptions>(options =>
        {
            options.CvIndexName = configuration["AzureAISearch__CvIndexName"]
                ?? throw new InvalidOperationException("AzureAISearch__CvIndexName is not configured.");
            options.JdIndexName = configuration["AzureAISearch__JdIndexName"]
                ?? throw new InvalidOperationException("AzureAISearch__JdIndexName is not configured.");
        });

        services.AddSingleton<IChunkStore, AzureAISearchService>();

        return services;
    }

    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());

        services.AddSingleton(sp =>
            KernelFactory.Create(configuration, sp.GetRequiredService<TokenCredential>()));

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            sp.GetRequiredService<SKernel>()
              .GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());

        return services;
    }
}
