# ADR-0011: URL-Segment API Versioning Strategy

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The current API ships as an initial version, but the design needs a concrete forward plan for breaking changes.

## Decision

We will use **URL-segment versioning** when the first breaking change is introduced.

- Launch routes remain unversioned and are treated as the logical **v1** contract.
- The first breaking change introduces explicit routes such as `/api/v2/articles`.
- Existing v1 routes remain supported for at least one deprecation window after v2 is introduced.

## Options Considered

### Option 1: No Versioning Plan
- **Pros:** Simplest at launch.
- **Cons:** Leaves breaking-change handling ambiguous.

### Option 2: URL-Segment Versioning
- **Pros:** Explicit, cache-friendly, easy to document, straightforward for OpenAPI generation.
- **Cons:** Route surface grows when multiple versions coexist.

### Option 3: Header-Based Versioning
- **Pros:** Clean URLs.
- **Cons:** Harder to debug manually and less obvious in logs and documentation.

## Consequences

### Positive
- Breaking changes have a documented migration path.
- Version identity is visible in routes, logs, caches, and OpenAPI documents.

### Negative
- Initial unversioned routes need a clear migration note once v2 exists.

## Implementation Notes

- Do not introduce `/api/v1/*` before a real breaking change exists.
- Once explicit versioning is introduced, publish separate OpenAPI documents per version.

## References

- Feature 06: RESTful API
- ADR-0010: OpenAPI Contract Generation with Swashbuckle
