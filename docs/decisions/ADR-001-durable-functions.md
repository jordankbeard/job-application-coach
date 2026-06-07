# ADR-001 — Use Azure Durable Functions for pipeline orchestration

**Status:** Accepted  
**Date:** 2026-06-07

## Context

The MVP pipeline has three sequential steps (ingest → gap analysis → bullet rewrite) where bullet rewriting is a fan-out across all CV bullets. Steps are long-running: PDF parsing + embedding can take 10–30s, and LLM calls add further latency per bullet. A naive synchronous HTTP handler would time out and has no retry capability.

## Decision

Use Azure Durable Functions (orchestrator + activity pattern) to manage the pipeline. The HTTP trigger returns a 202 with a status polling URL immediately; the orchestrator drives activities asynchronously.

## Consequences

- **Fan-out:** bullet rewrites run in parallel (one activity per bullet), dramatically reducing total rewrite time vs sequential
- **Retry:** each activity can be retried independently without restarting the whole pipeline
- **Status polling:** client gets a status URL to check progress — important when UI is added later
- **Added complexity:** Durable Functions has more moving parts than plain HTTP functions; serialisation constraints apply to orchestrator inputs/outputs
- **Local dev:** requires Azurite + Durable storage emulator for local testing
