# ADR-0001: Entity Framework Core Code-First with Repository and Unit of Work Patterns

**Date:** 2026-04-04
**Category:** data
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform persists three core entities (User, Article, DigitalAsset) in a relational database with referential integrity, indexes for query performance, and safe schema evolution (L1-011). The data access strategy must support LINQ-based queries for the MediatR handlers, provide testable abstractions, and work with the ASP.NET Core DI container.

The team needs to decide on both the ORM and the data access pattern. Direct `DbContext` usage in handlers is simpler but couples business logic to EF Core. Repository and Unit of Work patterns add abstraction but improve testability.

## Decision

We will use **Entity Framework Core** with a **code-first** approach for data access. The data layer is organized as:

1. **BlogDbContext** — central DbContext exposing `DbSet<User>`, `DbSet<Article>`, `DbSet<DigitalAsset>`.
2. **Entity Configurations** — `IEntityTypeConfiguration<T>` classes defining table mappings, constraints, and indexes.
3. **Repositories** — domain-specific interfaces (`IArticleRepository`, `IUserRepository`, `IDigitalAssetRepository`) encapsulating query logic.
4. **Unit of Work** — coordinates transactional boundaries across repositories, wrapping `SaveChangesAsync()` and explicit transaction management.

## Options Considered

### Option 1: EF Core Code-First with Repository + Unit of Work
- **Pros:** Code-first approach keeps the schema definition in C# alongside the application, LINQ queries are type-safe and translated to optimized SQL, entity configurations consolidate all mapping logic, repositories provide testable abstractions (mock `IArticleRepository` without `DbContext`), Unit of Work ensures atomic multi-entity operations, EF Core migrations provide versioned schema evolution.
- **Cons:** Repository pattern adds an abstraction layer over EF Core (which is itself a repository/UoW), risk of "leaky abstraction" where repositories expose `IQueryable` and leak EF Core behavior, more boilerplate than direct DbContext usage.

### Option 2: Direct DbContext in Handlers (No Repository)
- **Pros:** Less boilerplate, handlers directly query the DbContext, no abstraction overhead.
- **Cons:** Handlers are tightly coupled to EF Core (harder to test without an in-memory database), query logic is scattered across handlers, no centralized place for domain-specific queries.

### Option 3: Dapper (Micro-ORM)
- **Pros:** Full SQL control, lightweight, excellent performance for complex queries.
- **Cons:** No change tracking, no migration framework, manual SQL for all CRUD operations, no LINQ, more code for simple operations.

### Option 4: Raw ADO.NET
- **Pros:** Maximum control, no ORM overhead.
- **Cons:** Enormous boilerplate, no migration framework, manual mapping, error-prone.

## Consequences

### Positive
- Entity configurations define schema, constraints, and indexes in code — reviewable in PRs, version-controlled.
- Composite index on `(Published, DatePublished)` optimizes the most frequent query (published article listing).
- Unique constraints on `Slug` and `Email` enforced at the database level.
- `SaveChangesAsync` override auto-sets `CreatedAt`/`UpdatedAt` timestamps.
- Repositories encapsulate domain-specific queries (`GetPublishedAsync`, `GetBySlugAsync`, `SlugExists`).
- Unit of Work coordinates multi-entity operations (e.g., delete article + disassociate assets) atomically.

### Negative
- Repository abstraction adds indirection — simple queries go through an extra layer.
- Repositories must be careful not to expose `IQueryable` (leaks EF Core behavior to callers).
- EF Core change tracking adds memory overhead for read-heavy queries (mitigated by `AsNoTracking()` for reads).

### Risks
- Database provider choice (PostgreSQL vs SQL Server) is still open. EF Core abstracts the provider, but some features (UUID generation, full-text search) differ. The decision should be finalized before the first migration.

## Implementation Notes

- `BlogDbContext` registered as scoped service.
- Configurations applied via `modelBuilder.ApplyConfigurationsFromAssembly()`.
- Repositories return entities (not DTOs) — mapping to DTOs happens in handlers.
- Read queries use `AsNoTracking()` for performance.
- `UnitOfWork` exposes repository properties and `SaveChangesAsync`, `BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`.

## References

- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Repository Pattern — Martin Fowler](https://martinfowler.com/eaaCatalog/repository.html)
- L1-011: Data Integrity and Persistence
- L2-034: Database Migrations
- Feature 10: Data Persistence — Full design
