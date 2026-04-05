# ADR-0009: Digital Asset Management with Content Negotiation

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform allows authenticated users to upload images for use in articles (L1-008). Uploaded assets must be validated for safety (content inspection, not just file extension), stored reliably, and served with optimized delivery to meet web performance targets. Images should be served in modern formats (WebP, AVIF) when the browser supports them, with optional server-side resizing via query parameters (L2-029).

Image optimization is critical for Lighthouse Performance scores and Core Web Vitals (LCP < 2.5s, CLS < 0.1). Below-the-fold images should be lazy-loaded, and responsive `srcset` attributes should provide appropriately sized images for each viewport.

## Decision

We will implement a **server-side digital asset pipeline** that:
1. **Validates** uploads by inspecting magic bytes (not file extensions) — supports JPEG, PNG, WebP, GIF, AVIF up to 10MB.
2. **Stores** assets with unique generated filenames and metadata (dimensions, content type, file size) in the database.
3. **Serves** assets with HTTP content negotiation — returns AVIF or WebP when the client's `Accept` header supports them, falling back to the original format.
4. **Resizes** images on request via query parameter (`?w=800`) while maintaining aspect ratio.
5. **Caches** served assets with `Cache-Control: max-age=31536000, immutable`.
6. **Renders** responsive `<picture>` elements via `ImageTagHelper` with `srcset` for breakpoints 320, 640, 960, 1280, 1920.

## Options Considered

### Option 1: Server-Side Image Pipeline with Content Negotiation
- **Pros:** Security-first validation (magic bytes), content negotiation delivers optimal format per client, server-side resizing reduces bandwidth without client-side complexity, `ImageTagHelper` automates `<picture>` / `srcset` markup, immutable cache headers eliminate re-requests.
- **Cons:** Image processing (format conversion, resizing) adds CPU cost, storage grows with multiple format variants, image processing library dependency.

### Option 2: Client-Side Image Optimization (Cloudinary / Imgix)
- **Pros:** Offloads processing to a managed service, global CDN delivery, transformation via URL parameters.
- **Cons:** External service dependency and cost, vendor lock-in, latency for first request, requires API keys management, data leaves the platform.

### Option 3: Upload and Serve As-Is
- **Pros:** Simplest implementation, no image processing dependency.
- **Cons:** No modern format optimization, large images served at full resolution to all devices, fails Lighthouse image optimization audits, no content-type validation beyond extension.

## Consequences

### Positive
- Magic byte validation prevents extension spoofing attacks (a `.jpg` containing a script is rejected).
- Content negotiation serves AVIF (smallest) to supporting browsers, WebP to others, and original format as fallback.
- Responsive `<picture>` elements with `srcset` ensure devices download appropriately sized images.
- Below-fold images get `loading="lazy"` and `decoding="async"` automatically via `ImageTagHelper`.
- Immutable cache headers mean images are fetched once and cached indefinitely by browsers.

### Negative
- Image processing adds a library dependency (ImageSharp or SkiaSharp — see open questions).
- Multiple format variants increase storage requirements.
- First request for a resized/converted variant incurs processing time.

### Risks
- Image processing library choice is still open. ImageSharp has a restrictive license for SaaS; SkiaSharp depends on native binaries. This should be resolved before implementation.

## Implementation Notes

- `FileValidator` reads first bytes of the stream: JPEG (`FF D8 FF`), PNG (`89 50 4E 47`), WebP (`52 49 46 46...57 45 42 50`), GIF (`47 49 46 38`), AVIF (ISOBMFF with `ftypavif`).
- `ImageProcessor` handles: `ConvertFormat()`, `Resize()`, `GetDimensions()`.
- `AssetStorage` abstracts file persistence (local filesystem initially, cloud blob storage later).
- `ImageTagHelper` output: `<picture>` with `<source type="image/avif">`, `<source type="image/webp">`, and `<img>` fallback.
- Upload endpoint: `POST /api/digital-assets` (multipart/form-data, `[Authorize]`).
- Serving endpoint: `GET /assets/{filename}?w=800` (public, no auth).

## References

- [File Signatures (Magic Bytes)](https://en.wikipedia.org/wiki/List_of_file_signatures)
- [HTTP Content Negotiation](https://developer.mozilla.org/en-US/docs/Web/HTTP/Content_negotiation)
- L2-028: Upload Digital Asset
- L2-029: Serve Optimized Digital Assets
- L2-020: Image Optimization
- Feature 04: Digital Asset Management — Full design
