# ADR-0003: Production Deployment Topology with CDN and Object Storage

**Date:** 2026-04-04
**Category:** infrastructure
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The current design set lacked a concrete production topology, including whether a CDN is required and how digital assets are stored outside development.

## Decision

The production topology will use:

- **CDN/edge cache** in front of the ASP.NET Core origin for public HTML and static assets.
- **At least two application instances** behind a load balancer for high availability.
- **Object storage** for digital assets in production.
- **PostgreSQL primary database** with point-in-time recovery enabled.

Local development may continue to use a single app instance and local filesystem asset storage.

## Consequences

### Positive
- Public latency and availability assumptions are now explicit.
- Asset storage, CDN caching, and application scaling are aligned.

### Negative
- Production is intentionally more complex than local development.

## References

- Feature 03: Public Article Display
- Feature 04: Digital Asset Management
- Feature 07: Web Performance
- Feature 10: Data Persistence
