# ADR-002 — Use Semantic Kernel for LLM orchestration

**Status:** Accepted  
**Date:** 2026-06-07

## Context

The application needs to call Azure OpenAI for gap analysis and bullet rewriting, manage prompt templates, parse structured outputs, and connect to a vector store for retrieval-augmented generation. This logic could be written directly against the Azure OpenAI SDK, but that would require hand-rolling prompt management, retry logic, and vector store integration.

## Decision

Use Microsoft Semantic Kernel (SK) as the LLM orchestration layer throughout the application. Domain operations are expressed as SK plugins with `[KernelFunction]`-annotated methods. Prompt templates are stored as YAML files in the SK standard format.

## Consequences

- **Azure-native fit:** SK has first-class connectors for Azure OpenAI and Azure AI Search, matching the chosen infrastructure
- **Structured output:** SK supports `GetJsonSchemaForType<T>()` and prompt-level output schemas, reducing bespoke parsing code
- **Plugin model:** keeps LLM logic modular and independently testable; plugins can be swapped or extended without touching orchestrator code
- **Prompt templates:** YAML-based templates are versionable in git and editable without recompiling
- **Vendor lock-in:** SK abstracts the LLM provider, so switching from Azure OpenAI to OpenAI direct requires only config changes
- **SK version sensitivity:** SK API surface changed significantly through 1.x; pin NuGet versions explicitly
