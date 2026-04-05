# ADR-0003: Server-Side Rendering with ASP.NET Razor Pages

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The public-facing blog site must deliver complete HTML content without requiring JavaScript for content display (L2-017). The site must achieve a Lighthouse Performance score of 100 on mobile (L2-022), sub-200ms TTFB at P95 (L2-018), and be fully indexable by search engines and AI agents. The total JavaScript budget is 50KB gzipped, used only for progressive enhancements (theme toggle, mobile nav, analytics).

Client-side rendering frameworks (React, Angular, Blazor WASM) send a JavaScript bundle that must execute before content is visible, which contradicts these performance and SEO requirements.

## Decision

We will use **ASP.NET Core Razor Pages** for server-side rendering of all public-facing pages. Content is rendered to complete HTML on the server. No client-side rendering framework is used for the public site. JavaScript is limited to optional progressive enhancements loaded with `defer`.

## Options Considered

### Option 1: ASP.NET Razor Pages (SSR)
- **Pros:** HTML is complete on first byte — no JS required for content, natural fit with ASP.NET Core (same process, same DI container), Tag Helpers transform markup during rendering (images, resource hints, SEO tags), Razor syntax is straightforward for HTML-centric pages, zero client-side framework overhead.
- **Cons:** Each page request requires server rendering (mitigated by response caching), limited interactivity without JavaScript, no component model as rich as React/Angular for complex UIs (acceptable for a read-heavy blog).

### Option 2: Static Site Generation (SSG)
- **Pros:** Pre-rendered pages eliminate server rendering time entirely, minimal infrastructure (serve from CDN or static host), best possible TTFB.
- **Cons:** Requires a build/publish pipeline to regenerate pages when content changes, introduces latency between content publish and site update, more complex deployment model, harder to integrate with the same ASP.NET Core API server.

### Option 3: Blazor Server or Blazor WebAssembly
- **Pros:** C# end-to-end (no JavaScript), Blazor Server has SSR capabilities in .NET 8.
- **Cons:** Blazor WASM ships a large runtime (~2MB), Blazor Server requires a persistent SignalR connection, both add complexity beyond what a content site needs, the component model is overkill for rendering articles.

### Option 4: Next.js / Nuxt.js (SSR with React/Vue)
- **Pros:** Rich component model, SSR with hydration, large ecosystem.
- **Cons:** Introduces a separate Node.js process alongside the .NET API, hydration adds JavaScript overhead, two runtimes to deploy and maintain, increased operational complexity for a content site.

## Consequences

### Positive
- Every page works with JavaScript disabled — full content visible on first byte.
- Search engines and AI agents index complete HTML without executing JavaScript.
- The 50KB JS budget is easily met since JS is only for progressive enhancements.
- Razor Pages share the same process as the API, eliminating network calls for data fetching during SSR.
- Tag Helpers (ImageTagHelper, ResourceHintTagHelper, SeoMetaTagHelper) transform HTML during rendering, producing optimized output.

### Negative
- Each request requires server-side rendering (mitigated by in-memory response cache with 60s TTL).
- Interactive features (mobile nav, theme toggle) require vanilla JavaScript or minimal libraries.
- No hot module replacement or fast dev server — standard `dotnet watch` for development.

### Risks
- If the platform later requires highly interactive features (e.g., real-time comments, inline editing), a client-side framework may need to be introduced for those specific pages. SSG remains a viable future optimization if rendering cost becomes a bottleneck.

## Implementation Notes

- Pages: `ArticleListPage` at `/` and `/articles`, `ArticleDetailPage` at `/articles/{slug}`.
- Layout: Shared `_Layout.cshtml` with NavBar, Footer, and SEO metadata sections.
- Tag Helpers registered via `_ViewImports.cshtml`.
- Response caching middleware caches rendered HTML for 60s with `stale-while-revalidate=600`.
- Critical CSS inlined in `<head>`; non-critical CSS loaded asynchronously.

## References

- [ASP.NET Core Razor Pages](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/)
- L2-017: Server-Side Rendering with Minimal JavaScript
- L2-022: Core Web Vitals Compliance
- Feature 03: Public Article Display — Full design
- Feature 07: Web Performance — SSR Approach (Section 7.1)
