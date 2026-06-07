using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SKernel = Microsoft.SemanticKernel.Kernel;

namespace JobApplicationCoach.Infrastructure.Kernel;

public static class KernelFactory
{
    public static SKernel Create(IConfiguration configuration, TokenCredential credential)
    {
        var endpoint = configuration["AzureOpenAI__Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI__Endpoint is not configured.");

        var chatDeployment = configuration["AzureOpenAI__ChatDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAI__ChatDeployment is not configured.");

        var embeddingDeployment = configuration["AzureOpenAI__EmbeddingDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAI__EmbeddingDeployment is not configured.");

#pragma warning disable SKEXP0010 // AddAzureOpenAIEmbeddingGenerator is experimental but is the SK-recommended replacement for the deprecated ITextEmbeddingGenerationService
        return SKernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(chatDeployment, endpoint, credential)
            .AddAzureOpenAIEmbeddingGenerator(embeddingDeployment, endpoint, credential)
            .Build();
#pragma warning restore SKEXP0010
    }
}
