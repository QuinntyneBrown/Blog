# ADR-0002: Responsive Five-Breakpoint Layout System

**Date:** 2026-04-04
**Category:** frontend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The public site must deliver an exceptional reading experience across all device sizes (L1-002, L1-012). The layout must adapt from phones (< 576px) to large desktop monitors (>= 1200px) without horizontal scrolling, with appropriate column counts, spacing, and typography at each size. Article body text must be constrained to ~70 characters per line on large screens for optimal readability (L2-036).

A single responsive design must handle five target viewport categories with distinct layout behaviors for the article listing grid, article detail page, and navigation.

## Decision

We will implement a **fluid, responsive layout system** across five breakpoints using CSS Grid and media queries:

| Breakpoint | Name | Min Width | Listing Grid | Nav Style |
|------------|------|-----------|-------------|-----------|
| XS | Extra Small | < 576px | 1 column | Hamburger menu |
| SM | Small | >= 576px | 1 column | Hamburger menu |
| MD | Medium | >= 768px | 2 columns | Horizontal bar |
| LG | Large | >= 992px | 2 columns | Horizontal bar |
| XL | Extra Large | >= 1200px | 3 columns | Horizontal bar |

Article detail pages constrain body text to `max-width: 720px` (~70ch) centered on MD+ viewports, and full-width with 16px padding on XS/SM.

## Options Considered

### Option 1: CSS Grid with Mobile-First Media Queries
- **Pros:** Native CSS solution with no JavaScript dependency, CSS Grid handles multi-column layouts elegantly, mobile-first ensures small screens are the baseline (progressive enhancement), five breakpoints provide fine-grained control, works with server-rendered HTML — no JS required for layout.
- **Cons:** Five breakpoints require careful testing across viewport sizes, CSS Grid is not supported in IE11 (not a concern for a modern blog).

### Option 2: CSS Framework (Bootstrap / Tailwind)
- **Pros:** Pre-built responsive grid system, well-tested across browsers, utility classes speed development.
- **Cons:** Adds CSS framework dependency (Bootstrap ~22KB, Tailwind varies), framework breakpoints may not match the required five-breakpoint system, potential for unused CSS bloat.

### Option 3: Container Queries
- **Pros:** Component-level responsiveness, more flexible than viewport-based queries.
- **Cons:** Newer browser API with less universal support, more complex for page-level layout, viewport queries are better suited for the page-level grid and nav patterns required.

## Consequences

### Positive
- Layout adapts smoothly from phone to desktop with no horizontal scrolling at any width.
- Article listing grid transitions naturally: 1 → 2 → 3 columns as space permits.
- Navigation collapses to a hamburger menu on small screens with 44x44px touch targets (WCAG 2.1).
- Article body text at ~70ch width provides optimal line length for readability research.
- No JavaScript required for responsive behavior — pure CSS.
- Critical CSS for above-the-fold content can be inlined (supports Feature 07 Critical CSS Inlining).

### Negative
- Five breakpoints mean more CSS to write and test.
- Images must provide responsive variants for different viewport widths (handled by ImageTagHelper).

### Risks
- Edge cases at breakpoint boundaries may need fine-tuning. Thorough testing with browser dev tools at each breakpoint is required.

## Implementation Notes

- Mobile-first CSS: base styles for XS, then `@media (min-width: 576px)`, `@media (min-width: 768px)`, etc.
- Article listing: `display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 24px`.
- Article detail: `.article-body { max-width: 720px; margin: 0 auto; padding: 0 16px; }`.
- NavBar: horizontal at >= 768px, hamburger at < 768px with JavaScript toggle for menu visibility.
- Base font size: >= 16px, line height >= 1.5 (L2-036, L2-039).
- Touch targets: >= 44x44px for all interactive elements (L2-037).

## References

- [CSS Grid Layout — MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/CSS_grid_layout)
- [Responsive Web Design — Ethan Marcotte](https://alistapart.com/article/responsive-web-design/)
- L1-012: Responsive Layout System
- L2-035: Responsive Article Listing Page
- L2-036: Responsive Article Detail Page
- L2-037: Responsive Navigation
- Feature 03: Public Article Display — Full design
