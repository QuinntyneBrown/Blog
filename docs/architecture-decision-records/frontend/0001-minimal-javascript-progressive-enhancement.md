# ADR-0001: Minimal JavaScript with Progressive Enhancement

**Date:** 2026-04-04
**Category:** frontend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The public-facing blog site must deliver complete content without JavaScript (L2-017), achieve a Lighthouse Performance score of 100 on mobile (L2-022), and keep the total JavaScript bundle under 50KB gzipped. The site's primary purpose is content consumption (reading articles), which does not require client-side interactivity.

Modern web development trends favor heavy client-side frameworks (React, Angular, Vue), but these frameworks ship JavaScript bundles that add load time, delay content visibility, and increase complexity — all without benefit for a read-heavy content site.

## Decision

We will use **no client-side framework** for the public site. All content is server-rendered via Razor Pages. JavaScript is used only for **optional progressive enhancements** that improve the experience but are not required for content access:

- **Mobile navigation** — hamburger menu toggle (< 1KB).
- **Theme toggle** — light/dark mode switch (< 1KB).
- **Analytics** — Core Web Vitals reporting via `web-vitals` library (< 1KB).

All JavaScript is loaded with the `defer` attribute to avoid render-blocking. Total JavaScript budget: <= 50KB gzipped.

## Options Considered

### Option 1: Minimal Vanilla JavaScript (Progressive Enhancement)
- **Pros:** Total JS under 5KB for all enhancements, zero framework overhead, content works with JavaScript disabled, fastest possible page load, no hydration step, no virtual DOM, no build toolchain for JS, WCAG 2.1 Level AA compliance is easier without JS-dependent UI.
- **Cons:** Interactive features must be built from scratch (no component library), limited interactivity (acceptable for a content site), developers familiar with frameworks may find vanilla JS less ergonomic.

### Option 2: Alpine.js or htmx (Lightweight Libraries)
- **Pros:** Small footprint (Alpine ~15KB, htmx ~14KB), declarative interactivity, enhances server-rendered HTML.
- **Cons:** Adds a dependency for features achievable with vanilla JS, still a library to load/parse, Alpine.js requires inline directives that complicate CSP.

### Option 3: React / Angular / Vue (SPA Framework)
- **Pros:** Rich component model, large ecosystem, familiar to many developers.
- **Cons:** Minimum bundle size 30-100KB+ gzipped (React alone is ~40KB), requires hydration after SSR, JavaScript must execute before content is interactive, adds build complexity, overkill for a content site.

### Option 4: Blazor WebAssembly
- **Pros:** C# end-to-end.
- **Cons:** ~2MB WASM runtime download, completely inappropriate for a content site targeting 50KB JS budget.

## Consequences

### Positive
- The 50KB JavaScript budget is easily met with ~5KB total for all enhancements.
- No JavaScript framework to update, secure, or maintain.
- Content is visible immediately on first byte — no hydration, no loading skeletons.
- Works perfectly with JavaScript disabled (WCAG 2.1 Level AA, L2-039).
- Core Web Vitals are excellent by default: no JS blocking LCP, no hydration causing INP/CLS.
- Simpler build pipeline — no webpack/vite/esbuild configuration.

### Negative
- Adding complex interactive features in the future would require either vanilla JS or introducing a framework.
- No client-side routing (each page is a full server round-trip — mitigated by `stale-while-revalidate` caching and fast TTFB).
- Developers must write vanilla JavaScript for enhancements.

### Risks
- If the public site later requires highly interactive features (real-time comments, collaborative editing), a lightweight framework may need to be introduced for those specific components. This can be done incrementally without rewriting the entire site.

## Implementation Notes

- All JS files placed in `wwwroot/js/` with content-hashed filenames for immutable caching.
- Script tags use `defer` attribute: `<script src="/js/app.a1b2c3d4.js" defer></script>`.
- Mobile nav: `<button>` toggles `aria-expanded` and a CSS class on the nav element.
- Theme toggle: reads `prefers-color-scheme`, stores preference in `localStorage`, toggles a `data-theme` attribute on `<html>`.
- `web-vitals` library reports CLS, LCP, INP to an analytics endpoint.

## References

- [Progressive Enhancement — MDN](https://developer.mozilla.org/en-US/docs/Glossary/Progressive_Enhancement)
- [web-vitals Library](https://github.com/GoogleChrome/web-vitals)
- L2-017: Server-Side Rendering with Minimal JavaScript
- L2-022: Core Web Vitals Compliance
- ADR-0003 (backend): Server-Side Rendering with Razor Pages
