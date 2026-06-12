using Azure.Monitor.OpenTelemetry.Exporter;
using JobApplicationCoach.Core.Ingest;
using JobApplicationCoach.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Workaround: DurableTaskClientExtensions.CreateCheckStatusResponse serialises the
// management-URL payload using Azure.Core's JsonObjectSerializer, which writes to the
// response stream synchronously. Kestrel disallows synchronous IO by default when the
// ASP.NET Core integration is in use. This option re-enables it so the Durable library
// can complete the write without throwing InvalidOperationException.
// Tracked upstream: https://github.com/Azure/azure-functions-durable-extension/issues
builder.Services.Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true);

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
