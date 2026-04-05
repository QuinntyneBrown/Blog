# ADR-0008: Comprehensive SEO and Discoverability Strategy

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform must achieve a perfect SEO rating (L1-003) and be maximally discoverable by search engines, AI agents, feed readers, and automated bots (L1-004). This requires metadata embedded in server-rendered HTML — client-side rendered metadata is unreliable for crawlers. The platform must serve structured data, social sharing tags, machine-readable feeds, and crawler directives.

Seven distinct areas must be addressed: semantic HTML, JSON-LD structured data, Open Graph/Twitter Card meta tags, canonical URLs, XML sitemap, RSS/Atom feeds, and AI agent discoverability (llms.txt).

## Decision

We will implement a **server-side SEO pipeline** using Razor Tag Helpers and dedicated endpoint handlers:

1. **SeoMetaTagHelper** — generates `<title>`, `<meta description>`, canonical URL, Open Graph, and Twitter Card tags in every page's `<head>`.
2. **JsonLdGenerator** — emits Schema.org `Article` (detail pages) and `Blog` (listing page) structured data as `<script type="application/ld+json">`.
3. **SitemapGenerator** — serves dynamic XML sitemap at `/sitemap.xml` with all published articles.
4. **FeedGenerator** — serves RSS 2.0 at `/feed.xml` and Atom at `/atom.xml` with the 20 most recent articles.
5. **Static files** — `/robots.txt` (allow public, disallow `/api/`, sitemap directive) and `/llms.txt` (AI agent summary).

All metadata is embedded in the initial HTML response during server-side rendering.

## Options Considered

### Option 1: Server-Side SEO Pipeline (Tag Helpers + Dynamic Endpoints)
- **Pros:** All metadata is present in the initial HTML response, crawlers index complete information without JavaScript execution, Tag Helpers ensure consistency across all pages, dynamic sitemap and feeds update automatically when content changes, llms.txt provides forward-looking AI agent support.
- **Cons:** Multiple components to maintain (tag helper, JSON-LD generator, sitemap generator, feed generator), sitemap and feeds regenerated on each request (mitigated by response caching).

### Option 2: Client-Side SEO Injection (react-helmet / angular-meta)
- **Pros:** Centralized in the SPA framework.
- **Cons:** Requires JavaScript execution by crawlers, unreliable for many search engines and AI agents, contradicts SSR decision.

### Option 3: Third-Party SEO Service
- **Pros:** Managed solution, possibly with analytics.
- **Cons:** External dependency, potential latency, cost, less control over generated metadata.

## Consequences

### Positive
- Every page is fully SEO-optimized on first render — no JavaScript dependency.
- JSON-LD validates against Google's Rich Results Test with zero errors (L2-008).
- Open Graph and Twitter Card tags ensure rich previews when shared on social platforms (L2-009).
- Dynamic sitemap keeps search engines informed of content changes within one request cycle (L2-011).
- RSS and Atom feeds enable subscription via feed readers (L2-014).
- `llms.txt` positions the platform for emerging AI agent discovery patterns (L2-016).

### Negative
- Each tag helper and generator must be kept in sync with the data model (e.g., new fields on articles must be reflected in JSON-LD).
- Sitemap and feed generation add database queries on each request (mitigated by 60-second response cache).

### Risks
- The `llms.txt` convention is emerging and not yet standardized. If the convention changes or is abandoned, the file is trivial to update or remove.

## Implementation Notes

- `SeoMetaTagHelper` receives an `SeoMetadata` record from the page model with title, description, image URL, page URL, and page type.
- Title pattern: `{Article Title} | {Site Name}` truncated to 60 characters (L2-012).
- Description: article abstract truncated to 160 characters (L2-012).
- Canonical URLs: absolute, lowercase, no trailing slashes, no pagination query parameters (L2-010).
- Semantic HTML: `<article>`, `<header>`, `<main>`, `<nav>`, `<time>`, `<figure>` with proper heading hierarchy (L2-007).
- Mixed-case URLs 301-redirect to lowercase canonical URL (L2-015).

## References

- [Schema.org Article](https://schema.org/Article)
- [Open Graph Protocol](https://ogp.me/)
- [Sitemaps Protocol](https://www.sitemaps.org/protocol.html)
- [RSS 2.0 Specification](https://www.rssboard.org/rss-specification)
- [Atom Syndication Format (RFC 4287)](https://tools.ietf.org/html/rfc4287)
- L1-003, L1-004, L2-007 through L2-016
- Feature 05: SEO & Discoverability — Full design
