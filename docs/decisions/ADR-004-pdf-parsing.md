# ADR-004 — Use Azure Document Intelligence for PDF parsing

**Status:** Accepted  
**Date:** 2026-06-07

## Context

CVs and job descriptions are commonly submitted as PDFs. Parsing must preserve document structure (sections, headings, bullet points) so the chunking strategy can operate at the section level. Options: Azure Document Intelligence (Layout model), PdfPig (.NET), iTextSharp.

## Decision

Use Azure Document Intelligence with the **Layout** model. It returns a structured document object with paragraphs, roles (heading, list item, etc.), tables, and bounding boxes — enough to identify CV sections reliably without heuristics.

## Consequences

- **Structure preservation:** Layout model identifies headings and list items, enabling section-aware chunking (critical for hybrid chunking strategy — see ADR-005)
- **Azure-native:** consistent with the rest of the stack; one fewer third-party dependency
- **Cost:** Layout model is metered per page; acceptable for single-document analysis but worth monitoring
- **Latency:** adds a network call before chunking; acceptable since the pipeline is async (Durable Functions)
- **Plain text fallback:** if input is already plain text (not PDF), the parser is bypassed; `IDocumentParser` interface allows this without changing the activity code
