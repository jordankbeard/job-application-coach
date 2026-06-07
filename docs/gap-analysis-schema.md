# Gap Analysis Schema

This is the canonical response schema produced by `AnalyseGapActivity` and progressively enriched by `RewriteBulletsActivity`.

## JSON structure

```json
{
  "sessionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "analysedAt": "2026-06-07T10:00:00Z",
  "overallFitScore": 0.74,

  "matched": [
    {
      "skill": "Kubernetes",
      "confidenceScore": 0.92,
      "cvEvidence": "Managed 12-node K8s cluster across 3 environments",
      "jdRequirement": "Experience with container orchestration (Kubernetes preferred)"
    }
  ],

  "partiallyMatched": [
    {
      "skill": "Azure DevOps",
      "confidenceScore": 0.58,
      "cvEvidence": "Used Azure Pipelines for CI/CD",
      "jdRequirement": "Azure DevOps administration including boards and release management",
      "gap": "No evidence of board management or release pipeline ownership"
    }
  ],

  "missing": [
    {
      "skill": "Terraform",
      "jdRequirement": "Infrastructure as code using Terraform",
      "priority": "Must",
      "retrievedChunk": "Candidates must demonstrate IaC experience..."
    }
  ],

  "bullets": [
    {
      "bulletId": "b-001",
      "original": "Led a team of 5 engineers to deliver a microservices migration",
      "cvSection": "Experience — Senior Engineer, Acme Corp",
      "relevantJdSkills": ["leadership", "microservices", "delivery"],
      "rewritten": null,
      "rewriteStatus": "pending"
    }
  ]
}
```

## Field notes

- `overallFitScore` — weighted average: matched × 1.0 + partiallyMatched × 0.5, divided by total distinct JD requirements
- `bullets[].rewriteStatus` — `pending` | `complete` | `failed`; set by `RewriteBulletsActivity`
- `missing[].priority` — sourced from JD language: "must" / "essential" → `Must`; "preferred" / "nice to have" → `Nice`
- `partiallyMatched[].gap` — LLM-generated sentence describing what is evidenced vs what is required

## Evolution notes
> Update this section as the schema changes across phases.

| Version | Change |
|---|---|
| 0.1 | Initial design — Phase 1 MVP |
