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
    /// <summary>
    /// Returns a <see cref="DefaultAzureCredential"/> configured for both local development
    /// and Azure-hosted execution.
    ///
    /// <para>
    /// <b>Why ExcludeVisualStudioCredential / ExcludeSharedTokenCacheCredential?</b><br/>
    /// When debugging in Visual Studio the <c>VisualStudioCredential</c> fires before
    /// <c>AzureCliCredential</c> in the default chain. VS is signed in to Azure AI Foundry,
    /// so the token it supplies has audience <c>urn:ms.scopedToken</c> — a Foundry-internal
    /// scope — rather than <c>https://cognitiveservices.azure.com/</c> which Cognitive
    /// Services requires. Excluding the VS credential forces the chain to fall through to
    /// <c>AzureCliCredential</c> (i.e. <c>az login</c>), which requests the
    /// correct scope. In Azure, ManagedIdentityCredential fires well before either of these
    /// so the exclusions have no effect in production.
    /// </para>
    /// </summary>
    private static TokenCredential CreateCredential() =>
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = true,
        });

    public static IServiceCollection AddDocumentParsing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TryAddSingleton is idempotent — if AddSemanticKernel or AddVectorStore was called
        // first, the same credential instance is reused across all three clients.
        services.TryAddSingleton<TokenCredential>(_ => CreateCredential());

        services.AddSingleton(sp =>
        {
            var endpoint = new Uri(configuration["AzureDocumentIntelligence:Endpoint"]
                ?? throw new InvalidOperationException("AzureDocumentIntelligence:Endpoint is not configured."));

            return new DocumentIntelligenceClient(endpoint, sp.GetRequiredService<TokenCredential>());
        });

        services.AddSingleton<IDocumentParser, AzureDocumentParser>();

        return services;
    }

    public static IServiceCollection AddVectorStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<TokenCredential>(_ => CreateCredential());

        services.AddSingleton(sp =>
        {
            var endpoint = new Uri(configuration["AzureAISearch:Endpoint"]
                ?? throw new InvalidOperationException("AzureAISearch:Endpoint is not configured."));

            return new SearchIndexClient(endpoint, sp.GetRequiredService<TokenCredential>());
        });

        services.Configure<AzureAISearchOptions>(options =>
        {
            options.CvIndexName = configuration["AzureAISearch:CvIndexName"]
                ?? throw new InvalidOperationException("AzureAISearch:CvIndexName is not configured.");
            options.JdIndexName = configuration["AzureAISearch:JdIndexName"]
                ?? throw new InvalidOperationException("AzureAISearch:JdIndexName is not configured.");
        });

        services.AddSingleton<IChunkStore, AzureAISearchService>();

        return services;
    }

    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<TokenCredential>(_ => CreateCredential());

        services.AddSingleton(sp =>
            KernelFactory.Create(configuration, sp.GetRequiredService<TokenCredential>()));

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            sp.GetRequiredService<SKernel>()
              .GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());

        return services;
    }
}
