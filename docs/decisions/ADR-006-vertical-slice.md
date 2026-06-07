# ADR-006 — Vertical Slice Architecture for Core project

**Status:** Accepted  
**Date:** 2026-06-07

## Context

Two architectural styles were considered for organising `JobApplicationCoach.Core`:

**Clean Architecture** — horizontal layers (Domain, Application, Infrastructure). Works well for complex domain models and large teams, but tends to produce sprawling `Models/`, `Interfaces/`, and `Services/` folders where unrelated classes sit side-by-side purely because they share a layer.

**Vertical Slice Architecture** — code is organised by feature, not layer. Each slice owns its models, interfaces, and logic. Slices don't reach into each other; shared concerns live in Infrastructure.

## Decision

Use Vertical Slice Architecture for `JobApplicationCoach.Core`. Features map to folders: `Ingest/`, `GapAnalysis/`, `BulletRewrite/`. Each folder contains its own models, interfaces, SK plugins, and prompt templates.

`JobApplicationCoach.Infrastructure` remains a separate project because Azure AI Search and Document Intelligence are genuinely cross-cutting dependencies used by multiple slices.

MediatR is explicitly excluded. It is commonly paired with VSA for request dispatching, but Azure Durable Functions already provides that mechanism (orchestrator → activity). Adding MediatR would introduce an abstraction with no payoff.

## Consequences

- **Cohesion:** all code for a feature is in one place — no hunting across layer folders
- **Navigability:** "where is the gap analysis prompt?" → `GapAnalysis/Prompts/`
- **Independent testability:** each slice can be tested in isolation without pulling in unrelated code
- **No shared models folder:** models live with their feature; if a model is genuinely shared across slices it moves to a `Shared/` folder (to be created only if needed — YAGNI)
- **Infrastructure stays separate:** `IDocumentParser` and `IVectorStoreService` are defined in their respective slice (`Ingest/`, `GapAnalysis/`) and implemented in Infrastructure — satisfying the Dependency Inversion Principle
