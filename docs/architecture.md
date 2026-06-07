# Architecture

## Solution structure

```
JobApplicationCoach/
├── src/
│   ├── JobApplicationCoach.Functions/        # Azure Functions host (Durable)
│   │   ├── HttpTriggers/
│   │   │   ├── PipelineTrigger.cs            # POST /pipeline → 202
│   │   │   └── GetStatusTrigger.cs           # GET /pipeline/{id}/status
│   │   ├── Orchestrators/
│   │   │   └── PipelineOrchestrator.cs
│   │   ├── Activities/
│   │   │   ├── IngestDocumentActivity.cs
│   │   │   ├── AnalyseGapActivity.cs
│   │   │   └── RewriteBulletsActivity.cs
│   │   ├── host.json
│   │   ├── local.settings.json.template
│   │   └── Program.cs
│   │
│   ├── JobApplicationCoach.Core/             # Domain logic — no Azure deps
│   │   ├── Models/
│   │   │   ├── GapAnalysisResult.cs
│   │   │   ├── DocumentChunk.cs
│   │   │   ├── BulletRewrite.cs
│   │   │   └── PipelineRequest.cs
│   │   ├── Plugins/                          # Semantic Kernel plugins
│   │   │   ├── GapAnalysisPlugin.cs
│   │   │   └── BulletRewriterPlugin.cs
│   │   ├── Prompts/                          # SK YAML prompt templates
│   │   │   ├── GapAnalysis/
│   │   │   │   └── skprompt.yaml
│   │   │   └── BulletRewriter/
│   │   │       └── skprompt.yaml
│   │   └── Interfaces/
│   │       ├── IDocumentParser.cs
│   │       └── IVectorStoreService.cs
│   │
│   └── JobApplicationCoach.Infrastructure/   # Azure service implementations
│       ├── DocumentParsing/
│       │   └── AzureDocumentParser.cs        # Azure Document Intelligence
│       ├── VectorStore/
│       │   └── AzureAISearchService.cs       # AI Search, namespaces: cv / jd
│       └── Kernel/
│           └── KernelFactory.cs              # Builds SK kernel from config
│
└── tests/
    ├── JobApplicationCoach.Core.Tests/
    └── JobApplicationCoach.Functions.Tests/
```

## Data flow

```
HTTP POST /pipeline
        │
        ▼
PipelineTrigger (HTTP trigger)
  - Validates request
  - Starts Durable orchestrator
  - Returns 202 + statusUrl
        │
        ▼
PipelineOrchestrator
  ┌─────────────────────────────────────────────────────┐
  │ 1. IngestDocumentActivity (CV)                      │
  │    AzureDocumentParser → chunks → embed → AI Search │
  │                                                     │
  │ 2. IngestDocumentActivity (JD)                      │
  │    (same, different namespace)                      │
  │                                                     │
  │ 3. AnalyseGapActivity                               │
  │    Dual-namespace retrieval → GapAnalysisPlugin     │
  │    → GapAnalysisResult (bullets: status=pending)    │
  │                                                     │
  │ 4. RewriteBulletsActivity × N (fan-out)             │
  │    One activity per bullet, run in parallel         │
  │    BulletRewriterPlugin → fills rewritten field     │
  └─────────────────────────────────────────────────────┘
        │
        ▼
GapAnalysisResult (complete)
stored / returned via status endpoint
```

## Azure services

| Service | Role | Notes |
|---|---|---|
| Azure AI Foundry | Model deployments | GPT-4o (chat), text-embedding-3-small |
| Azure AI Search | Vector store | Two index namespaces: `cv`, `jd` |
| Azure Document Intelligence | PDF parsing | Layout model for structure preservation |
| Azure Functions v4 | Host | Isolated worker, .NET 8 |
| Azure Durable Functions | Pipeline orchestration | Fan-out for bullet rewrites |

## Key dependencies (NuGet)

```
Microsoft.SemanticKernel
Microsoft.SemanticKernel.Connectors.AzureAISearch
Microsoft.Azure.Functions.Worker
Microsoft.Azure.Functions.Worker.Extensions.DurableTask
Azure.AI.DocumentIntelligence
Azure.Search.Documents
```
