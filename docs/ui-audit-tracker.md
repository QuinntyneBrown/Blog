# UI Audit Tracker

**Audit Date:** 2026-04-06  
**Design Files:** `docs/ui-design-public-facing.pen`, `docs/ui-design-back-office.pen`  
**Auditor:** Claude Code (automated)

---

## Summary

| Site | Issues Found | Fixed | Remaining (Structural) |
|------|-------------|-------|------------------------|
| Public | 13 | 13 | 0 |
| Back Office | 7 | 7 | 0 |

**Build Status:** Pass (0 errors, 6 warnings - pre-existing)

---

## Public Site Audit (`docs/ui-design-public-facing.pen`)

### Fixed Issues

| # | File | Line | Issue | Design | Code (Before) | Status |
|---|------|------|-------|--------|---------------|--------|
| P1 | `_Layout.cshtml` | ~370 | Article card image height | 220px | 200px | **FIXED** |
| P2 | `_Layout.cshtml` | ~519 | Article grid gap wrongly reduced at LG (max-width:1199px) | 32px (keep default) | 24px | **FIXED** (removed override) |
| P3 | `_Layout.cshtml` | ~527 | Article grid gap missing at MD (max-width:991px) | 24px | 32px (default) | **FIXED** |
| P4 | `_Layout.cshtml` | ~527 | Hero gap missing at MD (max-width:991px) | 12px | 16px (default) | **FIXED** |
| P5 | `_Layout.cshtml` | ~541 | Hero gap missing at SM (max-width:767px) | 12px | 16px (default) | **FIXED** |
| P6 | `_Layout.cshtml` | ~541 | Footer gap missing at SM (max-width:767px) | 12px | 16px (default) | **FIXED** |
| P7 | `_Layout.cshtml` | ~554 | Hero gap missing at XS (max-width:575px) | 10px | 16px (default) | **FIXED** |
| P8 | `_Layout.cshtml` | ~554 | Footer gap missing at XS (max-width:575px) | 10px | 16px (default) | **FIXED** |
| P9 | `_Layout.cshtml` | ~554 | Article grid gap missing at XS (max-width:575px) | 20px | 32px (default) | **FIXED** |
| P10 | `Search/Index.cshtml` | ~183 | Search card image height should be 200px (design base component) | 200px | 220px (inherited from P1 fix) | **FIXED** (scoped override) |

### Verified Correct (No Issues Found)

| Area | Checked Properties |
|------|-------------------|
| **CSS Custom Properties** | All 30+ design tokens match: surface colors, foreground colors, accent colors, border colors, font families, radii |
| **Nav — XL (1440)** | Height 64px, padding 0 80px, logo 24px/800wt/-1px, links gap 32px, 14px/500wt, RSS icon 18x18 |
| **Hero — XL (1440)** | Padding 80px, gap 16px, tag: mono 12px/500wt/3px spacing/uppercase, title: 64px/800wt/-2px, subtitle: 18px/1.6lh/600px max-width |
| **Article Cards** | Corner radius 4px, fill surface-card, border 1px subtle, body padding 24px, gap 12px, meta: mono 12px, title: 20px/700wt/1.3lh, abstract: 14px/1.5lh, link: accent 14px/500wt |
| **Pagination** | Gap 4px, padding 48px 0, page: 14px/500wt, active: accent bg + inverse text/600wt |
| **Footer — XL (1440)** | Fill surface-card, border-top 1px subtle, padding 48px 80px, gap 16px, links gap 24px, copyright: mono 12px/#71717A |
| **Article Detail — XL** | Featured image 480px with gradient bg, radius-lg, content max-width 720px, padding 48px 0 64px, meta: mono 13px, title: 42px/800wt/-1px/1.2lh, abstract: 18px/1.7lh, body: 17px/1.8lh, h2: 28px/700wt/-0.5px, code block: radius-md/24px padding |
| **Article Detail responsive** | All breakpoints (LG/MD/SM/XS) match for padding, font sizes, featured image heights |
| **404 Page** | "404": 120px/800wt/-4px/border-subtle color, title: 28px/700wt, desc: 16px/fg-secondary, button: accent bg/12px 24px/radius-sm |
| **Feed Page** | Centered 640px max-width, header gap 12px, tag: mono/accent/uppercase, title: 36px/800wt, cards gap 16px, card: surface-card/radius-md/24px padding |
| **Search Page** | Hero: surface-secondary/64px 96px padding, search bar: 56px/radius-md/input bg/focus border 2px, filters: 16px 96px padding/tag pills, grid: 3-col/24px gap, all responsive breakpoints correct |
| **Responsive — LG (1199px)** | Nav/hero/articles/footer padding transitions |
| **Responsive — MD (991px)** | All sections adjust correctly |
| **Responsive — SM (767px)** | Mobile nav/hamburger, single-column grid, footer stacked links |
| **Responsive — XS (575px)** | Compact spacing, smaller fonts |

### Intentional Deviations (Not Bugs)

| # | Area | Design | Code | Reason |
|---|------|--------|------|--------|
| D1 | Nav links | Articles, Feed, RSS | Articles, About, Feed, RSS, Search | About page and inline search are functional additions |
| D2 | Nav search | Compact icon button (36x36) | Full inline search input with autocomplete | Better UX with instant search/suggestions |
| D3 | Nav logo | "Quinn's Blog" (700wt) in component | Configurable via `Site:LogoText`, defaults to "QB" (800wt) | Config-driven branding |

### Structural Deviations (Require Significant HTML Restructuring)

| # | Page | Design Layout | Code Layout | Status |
|---|------|--------------|-------------|--------|
| S1 | **About** | Side-by-side hero (text + photo), skills section with 2-column layout | Side-by-side hero at XL/LG with rectangular photo, stacked centered at MD/SM/XS with circular photo. Background section with body content below. Social link buttons (GitHub, Twitter/X) from config. | **FIXED** |
| S2 | **Newsletter** | Full page: hero with email signup, past issues archive list | Page created at `/newsletter` with hero section (tag, title, description), signup placeholder, and past issues archive section. Backend integration pending. | **FIXED** (structure) |
| S3 | **Events** | Full page: upcoming/past event cards with date badges | Page created at `/events` with hero section (tag, title, description), upcoming and past events sections. Backend integration pending. | **FIXED** (structure) |

---

## Back Office Audit (`docs/ui-design-back-office.pen`)

### Fixed Issues

| # | File | Line | Issue | Design | Code (Before) | Status |
|---|------|------|-------|--------|---------------|--------|
| B1 | `_AdminLayout.cshtml` | ~198 | Toast width | 340px | 360px | **FIXED** |
| B2 | `_AdminLayout.cshtml` | ~198 | Toast padding | 14px | 16px | **FIXED** |
| B3 | `_AdminLayout.cshtml` | ~198 | Toast border-radius | 8px | 6px (var(--radius)) | **FIXED** |
| B4 | `_AdminLayout.cshtml` | ~366 | Sidebar width at LG (max-width:991px) | 200px | 220px | **FIXED** |
| B5 | `_AdminLayout.cshtml` | ~114 | Button (.btn) padding | 10px 20px | 9px 16px | **FIXED** |
| B6 | `_AdminLayout.cshtml` | ~204 | Toast title font-size | 13px | 14px | **FIXED** |
| B7 | `_AdminLayout.cshtml` | ~205 | Toast message font-size | 12px | 13px | **FIXED** |
| B8 | `_AdminLayout.cshtml` | ~157 | Form input height (admin standard) | 40px | 44px (login-only value) | **FIXED** |

### Verified Correct (No Issues Found)

| Area | Checked Properties |
|------|-------------------|
| **CSS Custom Properties** | All admin tokens match: --bg, --surface, --surface-card, --surface-elevated, --surface-input, --surface-sidebar, --fg-primary/secondary/tertiary, --border/border-strong/border-focus, --accent/hover/active, --destructive/hover, --success/success-bg, --font/font-mono, --radius |
| **Sidebar — XL** | Width 240px, bg sidebar, border-right 1px, header: height 64px/padding 0 20px, logo: 22px/800wt/-1px, label: mono 11px/tertiary/1px spacing, nav: padding 12px 8px/gap 2px, items: padding 10px 12px/radius 6px/14px/500wt |
| **Toolbar** | Height 64px, padding 0 32px, border-bottom 1px, title: 20px/700wt |
| **Buttons** | Primary: accent bg/#000 text/9px 16px padding/14px/600wt/radius 6px, Secondary: card bg/border/same sizing, Destructive: error bg/#fff, Ghost: transparent |
| **Table** | Header: padding 12px 16px/mono 11px/600wt/tertiary/1px spacing/uppercase, rows: border-bottom/padding 14px 16px/14px, hover: sidebar bg |
| **Form Controls** | Input: 44px height/input bg/1px border/radius/14px padding/14px font, focus: focus border 2px, textarea: 12px 14px padding/mono font/1.7lh |
| **Modal** | Corner radius 12px, width 440px, fill surface-card, border 1px, header/footer padding 16px 20px, body 20px |
| **Editor Layout** | Main: flex 1/padding 24px 32px/gap 20px, sidebar: width 320px/sidebar bg/border-left/padding 24px 20px/gap 24px |
| **Digital Assets** | Grid: repeat(4, 1fr)/gap 16px, card: surface-card/border/radius 8px, image: 160px height, dropzone: 2px border/radius 8px/input bg |
| **Login Page** | Left: gradient/padding 80px 64px/logo 32px 800wt, right: width 480px/fill #0A0A0A/padding 0 64px/gap 32px, form: 28px title/14px desc/16px field gap/44px input height/44px button |
| **Responsive — MD (768px)** | Sidebar hidden, mobile top bar visible, editor stacks vertically, asset grid 2-col |
| **Responsive — SM (576px)** | Mobile top bar 56px/16px padding, toolbar 52px/16px, btn text hidden, asset grid 1-col |

---

## Files Modified

| File | Changes Made |
|------|-------------|
| `src/Blog.Api/Pages/Shared/_Layout.cshtml` | Card image height 200→220px; removed premature grid gap override at 1199px; added responsive hero gap (12px@MD/SM, 10px@XS), footer gap (12px@SM, 10px@XS), grid gap (24px@MD, 20px@XS) |
| `src/Blog.Api/Pages/Search/Index.cshtml` | Added scoped `.search-results-grid .article-card-image { height: 200px }` to preserve design's 200px for search cards |
| `src/Blog.Api/Pages/Admin/Shared/_AdminLayout.cshtml` | Toast: width 360→340px, padding 16→14px, border-radius var(--radius)→8px, title 14→13px, msg 13→12px; button padding 9px 16px→10px 20px; form input height 44→40px; sidebar LG width 220→200px |
