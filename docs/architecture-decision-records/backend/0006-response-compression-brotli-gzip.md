# ADR-0006: Response Compression with Brotli and Gzip

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform targets sub-200ms TTFB and a Lighthouse Performance score of 100 (L2-018, L2-022). Response payload size directly impacts transfer time, especially on slow connections. HTML pages, CSS, JavaScript, JSON API responses, XML sitemaps, and SVG assets are all compressible text formats that benefit significantly from compression.

Modern browsers universally support Gzip and increasingly support Brotli, which achieves better compression ratios at comparable decompression speed.

## Decision

We will compress all text-based responses using **Brotli (preferred) and Gzip (fallback)** via ASP.NET Core's built-in Response Compression middleware. Static assets are pre-compressed at build time (Brotli level 11); dynamic responses are compressed on-the-fly (Brotli level 4). The minimum response size for compression is 860 bytes to avoid overhead exceeding savings.

## Options Considered

### Option 1: Brotli + Gzip via ASP.NET Core Response Compression
- **Pros:** Built into ASP.NET Core, no external dependency, Brotli achieves 15-25% better compression than Gzip for HTML/CSS, pre-compression of static assets eliminates runtime CPU cost for the most-requested files, `Accept-Encoding` negotiation handled automatically.
- **Cons:** Dynamic Brotli compression at high levels is CPU-intensive (mitigated by using level 4 for dynamic), requires `EnableForHttps = true` (safe when combined with anti-CRIME measures like no user secrets in compressed responses).

### Option 2: Gzip Only
- **Pros:** Universal browser support, lower CPU cost than Brotli, simpler configuration.
- **Cons:** 15-25% worse compression ratios than Brotli, missing an easy optimization given that Brotli is supported by all modern browsers.

### Option 3: Reverse Proxy Compression (Nginx/Cloudflare)
- **Pros:** Offloads compression from the application server, CDN-level compression covers edge caching.
- **Cons:** Adds infrastructure dependency, CDN is deferred (see Feature 07 open questions), does not help in development or direct-to-origin scenarios.

## Consequences

### Positive
- HTML pages are typically 60-80% smaller after Brotli compression, reducing transfer time proportionally.
- Static assets pre-compressed at Brotli level 11 achieve maximum compression with zero runtime cost.
- Gzip fallback ensures all clients receive compressed responses regardless of Brotli support.
- Compression middleware is positioned first in the pipeline (outermost), ensuring all responses including cached ones are compressed.

### Negative
- Dynamic Brotli compression adds CPU overhead per request (mitigated by caching compressed responses).
- `EnableForHttps = true` is required since the site is HTTPS-only. This is safe because user secrets are never included in compressed response bodies.

### Risks
- If CPU becomes a bottleneck under high load, dynamic Brotli level can be reduced or disabled in favor of serving only pre-compressed static assets with Brotli and using Gzip for dynamic responses.

## Implementation Notes

- MIME types compressed: `text/html`, `text/css`, `application/javascript`, `application/json`, `image/svg+xml`, `application/xml`.
- Static assets: build step generates `.br` (level 11) and `.gz` alongside originals.
- Middleware order: CompressionMiddleware → ResponseCachingMiddleware → StaticFileMiddleware → routing.
- Minimum response size: 860 bytes.

## References

- [ASP.NET Core Response Compression](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression)
- [Brotli Compression](https://github.com/google/brotli)
- L2-041: Gzip/Brotli Compression
- Feature 07: Web Performance — Compression Configuration (Section 7.3)
