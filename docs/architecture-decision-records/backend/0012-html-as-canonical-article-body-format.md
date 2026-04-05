# ADR-0012: HTML as the Canonical Article Body Format

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The article-management and public-rendering designs need a single canonical storage format for article bodies. Supporting both Markdown and HTML increases editor, rendering, sanitization, and migration complexity.

## Decision

We will store article bodies as **sanitized HTML** in the database.

- The canonical persisted format is HTML.
- Any editor-side authoring experience that resembles Markdown must convert to HTML before persistence.
- Sanitization occurs at write time before the HTML is stored.

## Options Considered

### Option 1: Canonical HTML
- **Pros:** Direct fit for Razor Page rendering, straightforward sanitization pipeline, no render-time Markdown conversion.
- **Cons:** Harder to diff manually than plain Markdown.

### Option 2: Canonical Markdown
- **Pros:** Portable authoring format, easier text diffs.
- **Cons:** Requires render-time conversion and an additional sanitization stage.

### Option 3: Store Both Markdown and HTML
- **Pros:** Maximum flexibility.
- **Cons:** Doubles storage and synchronization complexity.

## Consequences

### Positive
- One canonical render-ready format across authoring, storage, feeds, and public rendering.
- Sanitization has a single unambiguous boundary: before persistence.

### Negative
- Markdown-native authoring is not first-class in the storage model.

## References

- L2-001: Create Article
- L2-025: Input Validation and Sanitization
- Feature 02: Article Management
- Feature 03: Public Article Display
