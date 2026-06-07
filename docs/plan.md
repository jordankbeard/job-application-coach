# Job Application Coach — Implementation Plan

## Status key
`[ ]` Not started · `[~]` In progress · `[x]` Done · `[!]` Blocked

---

## Phase 1 — MVP skeleton

### 1.1 Repo & solution setup
- [x] Initialise git repo and `.gitignore` (dotnet + Azure Functions)
- [x] Create solution file `JobApplicationCoach.slnx` (note: .slnx not .sln — newer XML format)
- [x] Create projects (Functions, Core, Infrastructure, Core.Tests, Functions.Tests)
- [x] Add NuGet packages — see notes below
- [x] Add `local.settings.json.template`
- [x] Configure `host.json` for Durable Functions
- [x] GitHub Actions CI (`ci.yml`) — build + test on push/PR to main

**NuGet decisions:**
- `Microsoft.SemanticKernel` → Core
- `Azure.AI.DocumentIntelligence 1.0.0` → Infrastructure
- `Azure.Search.Documents 12.0.0` → Infrastructure
- `Microsoft.Azure.Functions.Worker.Extensions.DurableTask 1.16.5` → Functions
- `Microsoft.SemanticKernel.Connectors.AzureAISearch` — **skipped** (prerelease only; using stable `Azure.Search.Documents` directly behind `IChunkStore`/`IChunkRetriever` interfaces instead)

### 1.2 Core models
- [x] `DocumentChunk` — chunk with parent-section metadata
- [x] `IngestRequest` — raw bytes + DocumentType enum + FileName
- [x] `ParsedParagraph` — intermediate Azure-agnostic paragraph model
- [x] `IDocumentParser` — abstraction: IngestRequest → IReadOnlyList\<ParsedParagraph\>
- [x] `IChunkStore` — write-side vector store abstraction (Ingest slice, ISP)
- [ ] `GapAnalysisResult` — full output schema (see [gap-analysis-schema.md](gap-analysis-schema.md))
- [ ] `BulletRewrite` — original / rewritten / rewriteStatus
- [ ] `IChunkRetriever` — read-side vector store abstraction (GapAnalysis slice, ISP)

### 1.3 Infrastructure wiring ✓
- [x] `AzureDocumentParser` — implements `IDocumentParser`; PDF via Document Intelligence Layout model, .txt via line-split fallback
- [x] `AzureAISearchService` — implements `IChunkStore`; batched embeddings via `IEmbeddingGenerator`, `MergeOrUpload` for idempotency
- [x] `KernelFactory` — builds SK Kernel with Azure AI Foundry chat + embedding deployments; `IEmbeddingGenerator<string,Embedding<float>>` extracted into DI
- [x] `InfrastructureServiceExtensions` — `AddDocumentParsing`, `AddSemanticKernel`, `AddVectorStore` extension methods
- [x] `Program.cs` wires all extension methods + `ChunkingService`
- [x] Level 1 DI composition tests (5 tests — container wires without hitting Azure); Level 2 deferred (see plan note)

### 1.4 Semantic Kernel plugins (stubs)
- [ ] `GapAnalysisPlugin` — `[KernelFunction] AnalyseAsync` → `GapAnalysisResult`
- [ ] `BulletRewriterPlugin` — `[KernelFunction] RewriteAsync` → `BulletRewrite`
- [ ] Prompt templates (`GapAnalysis.yaml`, `BulletRewriter.yaml`)

### 1.5 Durable Functions pipeline
- [x] `PipelineTrigger` — POST /pipeline → validates, starts orchestrator, returns 202 + statusUrl
- [x] `PipelineOrchestrator` — Ingest CV + JD in sequence (AnalyseGap + RewriteBullets fan-out TODO)
- [x] `IngestDocumentActivity` — IDocumentParser → ChunkingService → IChunkStore
- [ ] `AnalyseGapActivity` — IChunkRetriever dual-namespace retrieval → GapAnalysisPlugin
- [ ] `RewriteBulletsActivity` — fan-out per bullet → BulletRewriterPlugin
- [ ] `GetStatusTrigger` — GET /pipeline/{id}/status → Durable instance status

### 1.6 Azure deployment & smoke test
> Added at user's request — validate the wired skeleton against real Azure services before building GapAnalysis/BulletRewrite.
- [ ] Confirm AI Search index names from Foundry (resolves OQ-1)
- [ ] Fill `local.settings.json` with real connection strings
- [ ] Deploy Functions app to Azure (publish or GitHub Actions deploy job)
- [ ] POST a real CV + JD, verify chunks appear in AI Search index

### 1.7 Skeleton validation
- [x] Unit tests for `ChunkingService` (10 tests)
- [x] Unit tests for `IngestDocumentActivity` (5 tests, NSubstitute mocks)
- [x] Unit tests for `PipelineOrchestrator` (5 tests, NSubstitute mocks)
- [x] Level 1 DI composition tests (5 tests — all 26 tests passing)
- [ ] Unit tests for model serialisation/deserialisation
- [ ] End-to-end happy path with stubbed LLM responses

> **Level 2 integration tests (deferred):** Hit real Azure test resources to verify parse → chunk → embed → upsert. Requires dedicated Azure test indexes + CI secrets. See `CompositionRootTests.cs` TODO comment.

---

## Deferred work
| Item | Reason deferred | Notes |
|---|---|---|
| `.docx` / `.doc` support | Not in immediate scope | Azure DI Layout model natively supports `.docx` — add routing in `AzureDocumentParser.IsPlainText()` when needed. One-line change. |
| `GetStatusTrigger` | Not yet needed for smoke test | Durable's `CreateCheckStatusResponse` already provides status polling URLs in the 202 response |
| Phase 2 cover letter + chat | After Phase 1 stable | FR-05, FR-06 |
| Phase 3 evaluation pipeline | After Phase 2 | FR-08, FR-09, FR-10 |

---

## Open questions
| # | Question | Owner | Status |
|---|---|---|---|
| OQ-1 | Which Azure AI Search index names / tiers are already provisioned in Foundry? | Dev | Open — needed for §1.6 |
| OQ-2 | Chunking happens in `IngestDocumentActivity` via `ChunkingService` | Dev | **Resolved** |
| OQ-3 | Token budget per request — set SK execution settings defaults | Dev | Open — address in §1.4 |
