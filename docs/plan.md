# Job Application Coach — Implementation Plan

## Status key
`[ ]` Not started · `[~]` In progress · `[x]` Done · `[!]` Blocked

---

## Phase 1 — MVP skeleton

### 1.1 Repo & solution setup
- [ ] Initialise git repo and `.gitignore` (dotnet + Azure Functions)
- [ ] Create solution file `JobApplicationCoach.sln`
- [ ] Create projects (see [architecture.md](architecture.md) for structure)
- [ ] Add NuGet packages (Semantic Kernel, Durable Functions, Azure AI Search, Document Intelligence)
- [ ] Add `local.settings.json.template` — never commit actual settings
- [ ] Configure `host.json` for Durable Functions

### 1.2 Core models
- [ ] `GapAnalysisResult` — full schema (see [gap-analysis-schema.md](gap-analysis-schema.md))
- [ ] `DocumentChunk` — chunk with parent-section metadata
- [ ] `BulletRewrite` — original / rewritten / status
- [ ] `PipelineRequest` — CV text + JD text, session ID

### 1.3 Infrastructure wiring
- [ ] `KernelFactory` — builds Semantic Kernel with Azure AI Foundry endpoints (chat + embedding)
- [ ] `AzureDocumentParser` — wraps Azure Document Intelligence, returns structured text blocks
- [ ] `AzureAISearchService` — wraps SK AI Search memory connector, two index namespaces (`cv`, `jd`)
- [ ] Register all services in DI (`Program.cs` / `Startup`)

### 1.4 Semantic Kernel plugins (stubs)
- [ ] `GapAnalysisPlugin` — `[KernelFunction] AnalyseAsync(cvChunks, jdChunks)` → `GapAnalysisResult`
- [ ] `BulletRewriterPlugin` — `[KernelFunction] RewriteAsync(bullet, jdContext)` → `BulletRewrite`
- [ ] Prompt templates in `/Prompts/` folder (YAML, SK standard)

### 1.5 Durable Functions pipeline
- [ ] `PipelineTrigger` (HTTP trigger) — accepts `PipelineRequest`, starts orchestrator, returns 202 + status URL
- [ ] `PipelineOrchestrator` — sequences: Ingest → AnalyseGap → RewriteBullets (fan-out)
- [ ] `IngestDocumentActivity` — parse PDF/text → chunk → embed → upsert to AI Search
- [ ] `AnalyseGapActivity` — dual-namespace retrieval → SK gap analysis plugin → returns `GapAnalysisResult`
- [ ] `RewriteBulletsActivity` — per-bullet SK call (fanned out), populates `rewritten` field
- [ ] `GetStatusTrigger` (HTTP trigger) — polls Durable instance status

### 1.6 Skeleton validation
- [ ] End-to-end happy path with stubbed LLM responses (no real Azure calls)
- [ ] Unit tests for chunking logic
- [ ] Unit tests for model serialisation/deserialisation

---

## Phase 2 — Cover letter + multi-turn chat (v2)
> Not planned yet. Begin after Phase 1 is stable.

- FR-05 Cover letter generation
- FR-06 Multi-turn iteration chat

---

## Phase 3 — Evaluation pipeline (v3)
> Not planned yet.

- FR-08 Output evaluation scoring
- FR-09 Streaming responses
- FR-10 Session persistence

---

## Open questions
| # | Question | Owner | Status |
|---|---|---|---|
| OQ-1 | Which Azure AI Search index names / tiers are already provisioned in Foundry? | Dev | Open |
| OQ-2 | Should chunking happen inside the Activity or in a dedicated pre-processing step? | Dev | Open |
| OQ-3 | Token budget per request — need to set SK execution settings defaults | Dev | Open |
