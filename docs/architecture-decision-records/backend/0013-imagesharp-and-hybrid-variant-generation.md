# ADR-0013: ImageSharp and Hybrid Variant Generation

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The digital-asset pipeline needs a concrete image-processing library and a concrete strategy for generating responsive variants without unpredictable first-request latency on common sizes.

## Decision

We will use **SixLabors.ImageSharp** and a **hybrid generation strategy**:

- **Canonical variants** for the standard responsive widths (`320, 640, 960, 1280, 1920`) in AVIF, WebP, and original fallback are generated eagerly at upload time.
- **Non-canonical requested widths** are generated on demand and cached in storage for reuse.

## Options Considered

### Option 1: ImageSharp + Eager Canonical Variants
- **Pros:** Pure managed library, easy deployment, predictable performance for common sizes.
- **Cons:** Increased upload-time CPU and storage.

### Option 2: SkiaSharp + On-Demand Generation
- **Pros:** Strong runtime performance.
- **Cons:** Native dependency complexity and cold-request latency for common variants.

### Option 3: Serve Originals Only
- **Pros:** Simplest implementation.
- **Cons:** Fails performance and responsive-image goals.

## Consequences

### Positive
- Most user-facing image requests hit pre-generated optimized assets.
- The system still supports ad hoc width requests without requiring infinite eager generation.

### Negative
- Uploads do more work up front.
- Storage usage grows with variant count.

## Implementation Notes

- Production usage must comply with the applicable ImageSharp license terms for the deployment model.
- Generated variants are stored alongside the original asset in object storage.

## References

- Feature 04: Digital Asset Management
- Feature 07: Web Performance
- ADR-0009: Digital Asset Management with Content Negotiation
