# UI Audit Tracker

**Audit Date:** 2026-04-06  
**Design Files:** `docs/ui-design-public-facing.pen`, `docs/ui-design-back-office.pen`  
**Auditor:** Claude Code (automated)

---

## Summary

| Site | Issues Found | Fixed | Remaining | Severity |
|------|-------------|-------|-----------|----------|
| Public | 9 | 5 | 4 | See below |
| Back Office | TBD | TBD | TBD | TBD |

---

## Public Site Audit

### Fixed Issues

| # | File | Issue | Design Value | Code Value | Status |
|---|------|-------|-------------|------------|--------|
| 1 | `_Layout.cshtml:370` | Article card image height | 220px | 200px | **FIXED** |
| 2 | `_Layout.cshtml:519` | Article grid gap at LG breakpoint (1199px) wrongly overridden | 32px (keep default) | 24px (removed) | **FIXED** |
| 3 | `_Layout.cshtml:527` | Hero gap not responsive at MD (991px) | 12px | 16px (default) | **FIXED** |
| 4 | `_Layout.cshtml:527` | Article grid gap added at MD (991px) | 24px | 32px (default) | **FIXED** |
| 5 | `_Layout.cshtml:541` | Hero gap not responsive at SM (767px) | 12px | 16px (default) | **FIXED** |
| 6 | `_Layout.cshtml:541` | Footer gap not responsive at SM (767px) | 12px | 16px (default) | **FIXED** |
| 7 | `_Layout.cshtml:554` | Hero gap not responsive at XS (575px) | 10px | 16px (default) | **FIXED** |
| 8 | `_Layout.cshtml:554` | Footer gap not responsive at XS (575px) | 10px | 16px (default) | **FIXED** |
| 9 | `_Layout.cshtml:554` | Article grid gap at XS (575px) | 20px | 32px (default) | **FIXED** |
| 10 | `Search/Index.cshtml:183` | Search result card image height should stay at 200px (base component) | 200px | 220px (inherited from fix #1) | **FIXED** |

### Verified Correct (No Issues)

| Area | Details |
|------|---------|
| CSS Variables | All 30+ design tokens match between design variables and `:root` CSS custom properties |
| Nav (XL) | Height 64px, padding 0 80px, logo 24px/800wt/-1px spacing, links gap 32px, 14px/500wt |
| Hero (XL) | Padding 80px, gap 16px, tag mono 12px/500wt/3px spacing, title 64px/800wt/-2px, subtitle 18px/1.6lh/600px max |
| Article Cards | Corner radius 4px, border 1px subtle, body padding 24px, gap 12px, meta mono 12px, title 20px/700wt, abstract 14px/1.5lh |
| Pagination | Gap 4px, padding 48px 0, page buttons 14px/500wt, active state accent bg |
| Footer (XL) | Bg surface-card, border-top subtle, padding 48px 80px, gap 16px, links gap 24px, copyright mono 12px |
| Article Detail (XL) | Featured image 480px, gradient bg, content max-width 720px, meta mono 13px, title 42px/800wt, body 17px/1.8lh |
| Article Detail responsive | All breakpoints match: LG/MD/SM/XS padding, font sizes, image heights |
| 404 Page | Number 120px/800wt, title 28px/700wt, description 16px, button accent bg with arrow-left icon |
| Feed Page | Centered 640px, header gap 12px, cards gap 16px, card padding 24px, button accent bg |
| Search Page | Hero padding 64px 96px, search bar 56px height, filters bar, 3-col grid, all responsive breakpoints |

### Intentional Deviations (Not Bugs)

These differences between design and code are intentional enhancements:

| # | Area | Design | Code | Reason |
|---|------|--------|------|--------|
| D1 | Nav links | Articles, Feed, RSS icon | Articles, About, Feed, RSS, Search | About page and inline search added as features |
| D2 | Nav search | Compact icon button (36x36) | Full inline search input with autocomplete | Better UX with instant search |
| D3 | Nav logo | "Quinn's Blog" (700wt) on some pages | Configurable via `Site:LogoText`, defaults to "QB" (800wt) | Config-driven branding |

### Structural Deviations (Require Redesign)

These are layout-level differences that would require significant HTML restructuring:

| # | Page | Design Layout | Code Layout | Notes |
|---|------|--------------|-------------|-------|
| S1 | About | Side-by-side hero (text left, photo right), skills section with two columns | Centered single-column with circular profile image | Would require new HTML structure and CSS |
| S2 | Newsletter | Full page with hero, email signup form, past issues archive | Not implemented yet | Page does not exist in code |
| S3 | Events | Full page with upcoming/past events cards, date badges | Not implemented yet | Page does not exist in code |

---

## Back Office Audit

*(Pending - audit in progress)*

---

## Files Modified

| File | Changes |
|------|---------|
| `src/Blog.Api/Pages/Shared/_Layout.cshtml` | Fixed card image height, responsive hero/footer gaps, article grid gaps |
| `src/Blog.Api/Pages/Search/Index.cshtml` | Added scoped card image height override for search results |
