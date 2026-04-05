# ADR-0007: Layered HTTP Caching Strategy

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform targets sub-200ms TTFB at P95 (L2-018) and immutable caching for static assets (L2-019). Content changes infrequently — articles are published/updated occasionally while most requests are reads. A caching strategy must balance freshness (readers see updated content quickly) with performance (avoid re-rendering identical pages).

The platform serves two types of resources with different caching requirements: static assets (CSS, JS, fonts, images) that change only on deployment, and HTML pages that change when content is published or updated.

## Decision

We will implement a **layered caching strategy** with:
- **Static assets:** content-hashed filenames with `Cache-Control: max-age=31536000, immutable` (1 year).
- **HTML pages:** `Cache-Control: max-age=60, stale-while-revalidate=600` with in-memory response cache and ETag-based conditional requests.
- **API responses:** `Cache-Control: no-cache` (always revalidate) to ensure back-office clients see fresh data.
- **Explicit cache invalidation** via `ICacheInvalidator` when articles are published, updated, or deleted.

## Options Considered

### Option 1: Layered Caching (Content-Hashed Static + Short TTL HTML + In-Memory Cache)
- **Pros:** Static assets are cached indefinitely with zero revalidation overhead, HTML pages serve stale content for up to 10 minutes while revalidating (excellent perceived performance), in-memory response cache eliminates re-rendering for identical requests within the TTL, ETags enable 304 responses for conditional requests.
- **Cons:** In-memory cache does not share across multiple server instances (acceptable for initial single-instance deployment), 60-second TTL means content updates take up to 60 seconds to appear (acceptable for a blog).

### Option 2: No Caching (Render Every Request)
- **Pros:** Content is always fresh, simplest implementation.
- **Cons:** Every request incurs full rendering cost (database query + template rendering), unlikely to meet sub-200ms TTFB under load, wasteful for read-heavy workloads.

### Option 3: Distributed Cache (Redis)
- **Pros:** Shared cache across multiple instances, supports horizontal scaling.
- **Cons:** Adds Redis infrastructure dependency, network latency for cache reads, overkill for initial single-instance deployment.

## Consequences

### Positive
- Static assets are cached for 1 year — browsers never re-request them until deployment (cache busting via filename hash).
- Most HTML requests are served from in-memory cache, achieving sub-millisecond response times.
- `stale-while-revalidate` means browsers show cached content instantly while fetching updates in the background.
- ETags enable 304 Not Modified responses, saving bandwidth for unchanged pages.
- Explicit invalidation on publish/update ensures cache stays fresh when content changes.

### Negative
- In-memory cache is per-instance — scaling to multiple instances would require distributed caching.
- Content updates take up to 60 seconds to appear on the public site (acceptable for a blog).

### Risks
- If the platform scales horizontally, the in-memory cache will need to be replaced with a distributed cache (Redis). This is a known future migration path.

## Implementation Notes

- Cache profiles defined as named `CacheProfile` records: `HtmlPage` (60s + SWR 600s), `StaticAsset` (1yr, immutable), `ApiResponse` (no-cache).
- `ResponseCachingMiddleware` uses `IMemoryCache` keyed by URL + `Accept-Encoding`.
- `ICacheInvalidator.InvalidateAsync(string urlPattern)` evicts matching entries on content change.
- Static file build step: generate content-hashed filenames (e.g., `app.a1b2c3d4.css`).
- `ETagGenerator` computes SHA-256 hash of rendered HTML, truncated to 16 hex characters.

## References

- [HTTP Caching — MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching)
- [stale-while-revalidate](https://web.dev/stale-while-revalidate/)
- L2-019: HTTP Caching Headers
- Feature 07: Web Performance — Caching Strategy (Section 7.2)
