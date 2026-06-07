# ADR-003 — Use Azure AI Search as vector store

**Status:** Accepted  
**Date:** 2026-06-07

## Context

Document chunks from CV and JD need to be embedded and stored for semantic retrieval during gap analysis. Options considered: Azure AI Search, pgvector (PostgreSQL), Qdrant.

The user already has an Azure AI Search index provisioned and connected to their Azure AI Foundry project via the "Data + indexes" feature.

## Decision

Use Azure AI Search as the vector store. Two logical namespaces within the same index (or two separate indexes) separate CV chunks from JD chunks: `cv` and `jd`.

Integrated via Semantic Kernel's `AzureAISearchVectorStoreRecordCollection` connector.

## Consequences

- **No new infrastructure:** existing Foundry-connected index is reused
- **SK connector available:** `Microsoft.SemanticKernel.Connectors.AzureAISearch` provides a ready-made integration
- **Hybrid search:** Azure AI Search supports both vector and keyword search; hybrid mode can be enabled later without schema changes
- **Cost:** AI Search is metered by index size and query volume — monitor during development
- **Two-namespace strategy:** separate CV and JD into distinct collections/indexes to prevent cross-contamination during retrieval; the orchestrator explicitly queries both and merges results

## Open question

> OQ-1: Confirm exact index name(s) already provisioned in Foundry to use as defaults in `local.settings.json.template`.
