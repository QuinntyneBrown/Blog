# ADR-0003: PostgreSQL as the Primary Database

**Date:** 2026-04-04
**Category:** data
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The persistence design needs a single production database choice so provider-specific migrations, operational runbooks, backup strategy, and performance guidance can be made concrete.

## Decision

We will use **PostgreSQL** as the primary relational database for the initial release.

Additional decisions:
- Connection management uses **Npgsql built-in connection pooling**.
- **PgBouncer** and read replicas are deferred until scale justifies them.
- Production migrations run as a **separate CI/CD step before application rollout**, not as an always-on startup responsibility.

## Options Considered

### Option 1: PostgreSQL
- **Pros:** Strong OSS ecosystem, excellent .NET support via Npgsql, robust indexing and JSON capabilities, good fit for PITR and managed cloud offerings.
- **Cons:** Less Azure-native than SQL Server in some environments.

### Option 2: SQL Server
- **Pros:** Familiar in .NET shops, strong Azure integration.
- **Cons:** Higher licensing/operational overhead for this project and less consistency with the existing SQL examples.

## Consequences

### Positive
- The docs can align on one SQL dialect and one operational model.
- Backup, migrations, pooling, and future full-text search choices can all target PostgreSQL directly.

### Negative
- Teams standardized on SQL Server would need provider changes to switch later.

## References

- Feature 10: Data Persistence
- L1-011: Data Persistence and Schema Evolution
- L2-034: Database Migrations
