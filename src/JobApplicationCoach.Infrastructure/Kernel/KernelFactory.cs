using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System.ClientModel.Primitives;
using SKernel = Microsoft.SemanticKernel.Kernel;

namespace JobApplicationCoach.Infrastructure.Kernel;

public static class KernelFactory
{
    public static SKernel Create(IConfiguration configuration, TokenCredential credential)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");

        var chatDeployment = configuration["AzureOpenAI:ChatDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAI:ChatDeployment is not configured.");

        var embeddingDeployment = configuration["AzureOpenAI:EmbeddingDeployment"]
            ?? throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment is not configured.");

        // Share one AzureOpenAIClient across both SK services so a single HTTP connection
        // pool is used.
        //
        // RetryPolicy(0) disables the SDK's built-in exponential-backoff retries. Without
        // this, a missing deployment (404) causes the OpenAI client to retry 3 times before
        // surfacing the error, inflating each activity invocation by ~70 s during debugging.
        // Production retry behaviour is handled at the Durable orchestrator level via
        // TaskOptions / RetryPolicy on CallActivityAsync — keeping that concern in one place.
        //
        // Azure.AI.OpenAI 2.x is based on System.ClientModel, not Azure.Core, so the retry
        // policy is set via ClientPipelineOptions.RetryPolicy rather than Retry.MaxRetries.
        var clientOptions = new AzureOpenAIClientOptions
        {
            RetryPolicy = new ClientRetryPolicy(maxRetries: 0),
        };
        var openAiClient = new AzureOpenAIClient(new Uri(endpoint), credential, clientOptions);

#pragma warning disable SKEXP0010 // AddAzureOpenAIEmbeddingGenerator is experimental but is the SK-recommended replacement for the deprecated ITextEmbeddingGenerationService
        return SKernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(chatDeployment, openAiClient)
            .AddAzureOpenAIEmbeddingGenerator(embeddingDeployment, openAiClient)
            .Build();
#pragma warning restore SKEXP0010
    }
}
