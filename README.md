# Job Application Coach

AI-powered tool that analyses a CV against a job description, identifies skill gaps, and rewrites CV bullets to better match the role.

## Docs

- [Implementation plan](docs/plan.md)
- [Architecture](docs/architecture.md)
- [Gap analysis schema](docs/gap-analysis-schema.md)
- [Architecture decisions](docs/decisions/README.md)

## Stack

- **Runtime:** .NET 8, Azure Functions v4 (isolated worker)
- **Orchestration:** Azure Durable Functions
- **LLM layer:** Semantic Kernel → Azure AI Foundry (GPT-4o, text-embedding-3-small)
- **Vector store:** Azure AI Search (namespaces: `cv`, `jd`)
- **PDF parsing:** Azure Document Intelligence (Layout model)

## Getting started

> Prerequisites: .NET 8 SDK, Azure Functions Core Tools v4, Azurite (local storage emulator)

1. Copy `src/JobApplicationCoach.Functions/local.settings.json.template` → `local.settings.json`
2. Fill in your Azure AI Foundry and AI Search connection strings
3. `func start` from the Functions project directory
