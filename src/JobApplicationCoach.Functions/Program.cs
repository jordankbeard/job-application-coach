using Azure.Monitor.OpenTelemetry.Exporter;
using JobApplicationCoach.Core.Ingest;
using JobApplicationCoach.Infrastructure;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Services.AddDocumentParsing(builder.Configuration);
builder.Services.AddSemanticKernel(builder.Configuration);
builder.Services.AddVectorStore(builder.Configuration);
builder.Services.AddSingleton<ChunkingService>();

builder.Build().Run();
