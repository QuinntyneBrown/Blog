# ADR-0003: SQL Server as Primary Database

**Date:** 2026-04-04
**Category:** data
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform uses Entity Framework Core with a code-first approach (ADR data/0001). EF Core abstracts the database provider, but the choice of relational database affects UUID generation strategy, data types, connection pooling, hosting costs, and operational tooling. The decision was deferred during initial design and flagged as a risk in ADR data/0001.

Two candidates were evaluated: PostgreSQL and SQL Server.

## Decision

We will use **SQL Server** as the primary relational database, accessed via the `Microsoft.EntityFrameworkCore.SqlServer` provider.

## Options Considered

### Option 1: SQL Server
- **Pros:** First-class integration with the .NET ecosystem and Azure (Azure SQL Database, Managed Instance), familiar tooling for .NET teams (SSMS, Azure Data Studio), strong enterprise support and documentation, `NEWSEQUENTIALID()` for clustered index-friendly GUIDs, `DATETIME2` with 100-nanosecond precision, built-in full-text search via `CONTAINS`/`FREETEXT`.
- **Cons:** Higher cloud hosting cost compared to PostgreSQL, proprietary licensing for self-hosted deployments, less common in open-source ecosystems.

### Option 2: PostgreSQL
- **Pros:** Open-source with no licensing cost, excellent JSON/JSONB support, lower cloud hosting cost (e.g., Azure Database for PostgreSQL Flexible Server), strong community and extension ecosystem, `gen_random_uuid()` for native UUID generation.
- **Cons:** Requires Npgsql provider (third-party, though well-maintained), slightly less integrated with Azure-native tooling, different operational patterns for .NET teams accustomed to SQL Server.

## Consequences

### Positive
- Tight integration with Azure for hosting (Azure SQL Database) with built-in backups, geo-replication, and auto-tuning.
- Familiar tooling and operational patterns for .NET developers.
- `NEWSEQUENTIALID()` produces sequential GUIDs that avoid clustered index fragmentation.
- `DATETIME2` provides higher precision than `datetime` and stores UTC timestamps cleanly.
- Built-in full-text search capabilities available if needed (see Feature 10 open question #6).

### Negative
- Higher hosting cost than PostgreSQL equivalents.
- Proprietary — if self-hosting is desired in the future, licensing applies.

## Implementation Notes

- EF Core provider: `Microsoft.EntityFrameworkCore.SqlServer`
- Primary key generation: `UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID()`
- Timestamps: `DATETIME2 DEFAULT SYSUTCDATETIME()`
- Boolean mapping: `BIT` (0/1)
- Text columns: `NVARCHAR(n)` for bounded, `NVARCHAR(MAX)` for unbounded
- Delete restrict: `ON DELETE NO ACTION` (SQL Server equivalent of `RESTRICT`)
- Connection string configured via `appsettings.json` / environment variables

## References

- [SQL Server on Azure](https://learn.microsoft.com/en-us/azure/azure-sql/)
- [EF Core SQL Server Provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)
- ADR data/0001: EF Core Code-First with Repository Pattern
- Feature 10: Data Persistence
