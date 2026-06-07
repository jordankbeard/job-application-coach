using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Search.Documents.Indexes;
using JobApplicationCoach.Core.Ingest;
using JobApplicationCoach.Infrastructure.DocumentParsing;
using JobApplicationCoach.Infrastructure.Kernel;
using JobApplicationCoach.Infrastructure.VectorStore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SKernel = Microsoft.SemanticKernel.Kernel;

namespace JobApplicationCoach.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddDocumentParsing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            var endpoint = new Uri(configuration["AzureDocumentIntelligence__Endpoint"]
                ?? throw new InvalidOperationException("AzureDocumentIntelligence__Endpoint is not configured."));

            var key = new AzureKeyCredential(configuration["AzureDocumentIntelligence__ApiKey"]
                ?? throw new InvalidOperationException("AzureDocumentIntelligence__ApiKey is not configured."));

            return new DocumentIntelligenceClient(endpoint, key);
        });

        services.AddSingleton<IDocumentParser, AzureDocumentParser>();

        return services;
    }

    public static IServiceCollection AddVectorStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(_ =>
        {
            var endpoint = new Uri(configuration["AzureAISearch__Endpoint"]
                ?? throw new InvalidOperationException("AzureAISearch__Endpoint is not configured."));

            var key = new AzureKeyCredential(configuration["AzureAISearch__ApiKey"]
                ?? throw new InvalidOperationException("AzureAISearch__ApiKey is not configured."));

            return new SearchIndexClient(endpoint, key);
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
        services.AddSingleton(_ => KernelFactory.Create(configuration));

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            sp.GetRequiredService<SKernel>()
              .GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());

        return services;
    }
}
