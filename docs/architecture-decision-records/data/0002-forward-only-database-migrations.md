# ADR-0002: Forward-Only Database Migrations with EF Core

**Date:** 2026-04-04
**Category:** data
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform's database schema will evolve as features are added and refined. Schema changes must be safe, versioned, and reproducible across environments (development, CI, staging, production). Rolling back database changes in production is risky — it can cause data loss, especially when columns or tables are dropped (L2-034).

The team needs a migration strategy that minimizes risk of data loss, supports concurrent development (multiple developers creating migrations), and integrates with CI/CD pipelines.

## Decision

We will use **EF Core Migrations** with a **forward-only policy** in production:

1. All schema changes are managed through EF Core migrations.
2. Migrations follow a timestamped naming convention: `YYYYMMDDHHMMSS_DescriptiveName`.
3. Migrations are forward-only in production — the `Down()` method is implemented for development convenience but never executed in production.
4. To fix a problematic migration, a new corrective migration is created rather than rolling back.
5. Destructive changes (dropping columns/tables) require a multi-step migration: add new → migrate data → drop old in a subsequent migration.
6. The full migration chain must apply cleanly to a fresh database (verified in CI).

## Options Considered

### Option 1: EF Core Migrations (Forward-Only)
- **Pros:** Schema defined in C# alongside the application, migration history tracked in `__EFMigrationsHistory` table, idempotent by design (re-running is a no-op), timestamped naming prevents ordering conflicts, CI can verify the full chain on a fresh database, `dotnet ef migrations script --idempotent` generates deployment SQL.
- **Cons:** Requires EF Core tooling (`dotnet ef`), migration files accumulate over time, concurrent migration creation by multiple developers can cause conflicts (mitigated by timestamps).

### Option 2: Manual SQL Scripts
- **Pros:** Full SQL control, no ORM dependency, explicit and reviewable.
- **Cons:** No automatic tracking of applied migrations, manual ordering, easy to apply out of order, no automatic `Down()` for development, more error-prone.

### Option 3: Third-Party Migration Tools (FluentMigrator, DbUp)
- **Pros:** Dedicated migration tools, flexible execution strategies.
- **Cons:** Additional dependency alongside EF Core, redundant when EF Core already provides migration support, different DSL to learn.

## Consequences

### Positive
- Schema evolution is version-controlled and reviewable in pull requests.
- Forward-only policy eliminates data loss risk from production rollbacks.
- Idempotent migrations are safe to re-run (the history table tracks what has been applied).
- CI pipeline verifies the complete migration chain against a fresh database on every build.
- Timestamped naming prevents merge conflicts when multiple developers create migrations.
- Multi-step destructive changes (add → migrate → drop) protect against data loss.

### Negative
- Migration files accumulate over time (can be squashed periodically in development).
- Forward-only means a bad migration requires a corrective migration rather than a simple rollback.
- Developers must remember to use timestamped names for migrations.

### Risks
- If a migration is applied in production with a bug, the corrective migration must handle the inconsistent state. Staging environment testing is critical before production deployment.

## Implementation Notes

- Migration naming: `dotnet ef migrations add 20260404120000_InitialCreate --project src/Blog.Api`.
- CI pipeline: `dotnet ef migrations script --idempotent` generates SQL artifact.
- Test pipeline: applies all migrations to a fresh database (Docker container) then runs integration tests.
- Staging: `MigrationRunner` hosted service applies pending migrations on startup.
- Production: migrations applied as a separate CI/CD step before deploying the new application version.
- Migration rules: additive changes preferred (add columns with defaults or nullable), no data loss operations without multi-step migration.

## References

- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- L2-034: Database Migrations
- Feature 10: Data Persistence — Migration Strategy (Section 7)
