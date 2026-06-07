# ADR-005 — Hybrid chunking strategy (sentence-level + parent context)

**Status:** Accepted  
**Date:** 2026-06-07

## Context

Chunking strategy directly affects retrieval quality. Options from the requirements spec:

| Strategy | Tradeoff |
|---|---|
| Semantic (section-level) | Chunks vary widely in size |
| Fixed token window | May split mid-sentence |
| Sentence-level | Loses surrounding context |
| **Hybrid (recommended)** | More complex, but precision + context |

## Decision

Use **hybrid chunking**: each chunk is sentence-level (or bullet-level for CV), but the parent section (heading + surrounding paragraph) is stored as metadata on each chunk. When a chunk is retrieved, the parent context is appended to the LLM prompt to restore surrounding meaning.

Implementation:

1. Azure Document Intelligence returns paragraphs with roles (`sectionHeading`, `listItem`, etc.)
2. Group paragraphs by the nearest preceding `sectionHeading`
3. For each list item / sentence within a section, create one `DocumentChunk`
4. Store the section heading + first ~100 tokens of the section body as `parentContext` metadata

## Consequences

- **Precise retrieval:** small chunks → accurate vector match
- **Full context for LLM:** parent metadata restores meaning without bloating the index
- **Section-aware:** headings from Document Intelligence make section detection reliable (vs regex heuristics on raw text)
- **Fixed-window fallback:** if Document Intelligence returns insufficient structure (e.g. a badly formatted PDF), fall back to 256-token fixed windows with 10% overlap
- **Implementation complexity:** chunking logic is non-trivial; unit test thoroughly before wiring to the full pipeline (see plan.md §1.6)
