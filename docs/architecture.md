# Architecture

## Architectural style: Vertical Slice

Each feature (Ingest, GapAnalysis, BulletRewrite) owns its models, interfaces, and business logic in a self-contained folder. Infrastructure is kept as a separate project because Azure AI Search and Document Intelligence are genuinely cross-cutting. MediatR is intentionally excluded — Durable Functions already provides dispatch.

See [ADR-006](decisions/ADR-006-vertical-slice.md).

---

## Solution structure

```
JobApplicationCoach/
├── src/
│   ├── JobApplicationCoach.Functions/        # Azure Functions host (Durable)
│   │   ├── HttpTriggers/
│   │   │   ├── PipelineTrigger.cs            # POST /pipeline → 202 + statusUrl
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
│   ├── JobApplicationCoach.Core/             # Organised by feature slice
│   │   ├── Ingest/
│   │   │   ├── IngestRequest.cs              # Input: raw CV + JD bytes/text
│   │   │   ├── DocumentChunk.cs              # Chunk with parent-section metadata
│   │   │   ├── IDocumentParser.cs            # Abstraction over Document Intelligence
│   │   │   └── ChunkingService.cs            # Hybrid chunking logic
│   │   ├── GapAnalysis/
│   │   │   ├── GapAnalysisRequest.cs         # Input: session ID
│   │   │   ├── GapAnalysisResult.cs          # Full output schema (see gap-analysis-schema.md)
│   │   │   ├── IVectorStoreService.cs        # Abstraction over AI Search
│   │   │   ├── GapAnalysisPlugin.cs          # SK [KernelFunction]
│   │   │   └── Prompts/
│   │   │       └── GapAnalysis.yaml          # SK prompt template
│   │   └── BulletRewrite/
│   │       ├── BulletRewriteRequest.cs       # Input: single bullet + JD context
│   │       ├── BulletRewrite.cs              # Output: original + rewritten + status
│   │       ├── BulletRewriterPlugin.cs       # SK [KernelFunction]
│   │       └── Prompts/
│   │           └── BulletRewriter.yaml       # SK prompt template
│   │
│   └── JobApplicationCoach.Infrastructure/   # Cross-cutting Azure implementations
│       ├── DocumentParsing/
│       │   └── AzureDocumentParser.cs        # Implements IDocumentParser
│       ├── VectorStore/
│       │   └── AzureAISearchService.cs       # Implements IVectorStoreService
│       └── Kernel/
│           └── KernelFactory.cs              # Builds SK Kernel from config
│
└── tests/
    ├── JobApplicationCoach.Core.Tests/       # Per-slice unit tests
    └── JobApplicationCoach.Functions.Tests/  # Activity + orchestrator tests
```

---

## Data flow

```
HTTP POST /pipeline
        │
        ▼
PipelineTrigger (HTTP trigger)
  - Validates PipelineRequest
  - Starts Durable orchestrator instance
  - Returns 202 + statusUrl
        │
        ▼
PipelineOrchestrator
  ┌─────────────────────────────────────────────────────┐
  │ 1. IngestDocumentActivity (CV)                      │
  │    IDocumentParser → chunks → embed → AI Search     │
  │                                                     │
  │ 2. IngestDocumentActivity (JD)                      │
  │    (same, different namespace)                      │
  │                                                     │
  │ 3. AnalyseGapActivity                               │
  │    IVectorStoreService dual-retrieval               │
  │    → GapAnalysisPlugin → GapAnalysisResult          │
  │      (bullets: rewriteStatus = pending)             │
  │                                                     │
  │ 4. RewriteBulletsActivity × N  ← fan-out           │
  │    One activity per bullet, parallel                │
  │    BulletRewriterPlugin → fills rewritten field     │
  └─────────────────────────────────────────────────────┘
        │
        ▼
GapAnalysisResult (complete)
returned via GET /pipeline/{id}/status
```

---

## Azure services

| Service | Role | Notes |
|---|---|---|
| Azure AI Foundry | Model deployments | GPT-4o (chat), text-embedding-3-small |
| Azure AI Search | Vector store | Two namespaces: `cv`, `jd` |
| Azure Document Intelligence | PDF parsing | Layout model — preserves headings/bullets |
| Azure Functions v4 | Host | Isolated worker, .NET 8 |
| Azure Durable Functions | Pipeline orchestration | Fan-out for bullet rewrites |

---

## NuGet packages

| Package | Project | Notes |
|---|---|---|
| `Microsoft.SemanticKernel` | Core | LLM orchestration, plugin model |
| `Azure.AI.DocumentIntelligence` | Infrastructure | PDF parsing |
| `Azure.Search.Documents` | Infrastructure | AI Search SDK (stable, no prerelease) |
| `Microsoft.Azure.Functions.Worker` | Functions | Isolated worker host |
| `Microsoft.Azure.Functions.Worker.Extensions.DurableTask` | Functions | Durable orchestrator + activities |
