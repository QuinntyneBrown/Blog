# ADR-0002: Vertical Slice Architecture with MediatR

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform needs a code organization strategy that keeps features cohesive, supports a CQRS-style separation of reads and writes, and enables cross-cutting concerns (validation, logging) to be applied uniformly without scattering them across controllers and services. The API serves both back-office and public consumers with distinct read/write patterns.

Traditional layered architecture (Controller → Service → Repository) leads to large service classes and scattered feature logic. We need an approach where adding a new feature does not require modifications across multiple layers.

## Decision

We will organize the codebase using **Vertical Slice Architecture** powered by **MediatR** as the in-process mediator. Each feature (command or query) is a self-contained slice with its own request, handler, validator, and DTO. Cross-cutting concerns are implemented as MediatR pipeline behaviors.

## Options Considered

### Option 1: Vertical Slice Architecture with MediatR
- **Pros:** Each feature is a single cohesive unit (request + handler + validator + DTO), adding features requires touching only one folder, MediatR pipeline behaviors apply cross-cutting concerns (validation, logging) uniformly, natural CQRS separation — commands and queries are distinct types, controllers become thin dispatchers.
- **Cons:** Indirection through the mediator can obscure call flow for developers unfamiliar with the pattern, slight overhead from the MediatR dispatch pipeline, risk of handlers growing too large if not disciplined about decomposition.

### Option 2: Traditional Layered Architecture (Controller → Service → Repository)
- **Pros:** Well-understood pattern, straightforward call hierarchy, easy to follow for new developers.
- **Cons:** Services grow into god classes as features multiply, cross-cutting concerns (validation, logging) must be manually applied in each service method, no natural CQRS boundary, modifications to one feature often touch controller + service + repository layers.

### Option 3: Clean Architecture with Use Cases
- **Pros:** Strong dependency inversion, domain isolation, testable business logic.
- **Cons:** More ceremony and abstractions for a relatively simple domain (articles, users, assets), the domain model for a blog is not complex enough to benefit from a full domain-driven design approach, more interfaces and layers than MediatR slices.

## Consequences

### Positive
- Features are self-contained: `CreateArticle.cs` contains the command, handler, and validator in one file or folder.
- `ValidationBehavior` runs FluentValidation on every request automatically — no manual validation calls.
- `LoggingBehavior` logs every request/response with consistent structure.
- Controllers are thin (3-5 lines per action: deserialize → send to MediatR → return result).
- Testing is simplified — each handler can be tested in isolation.

### Negative
- Developers must understand the MediatR pipeline to follow request flow.
- Debugging requires stepping through pipeline behaviors before reaching the handler.
- The mediator pattern can be overused — not every method call needs to go through MediatR.

### Risks
- MediatR is a third-party library. If abandoned, the pipeline behavior infrastructure would need replacement. However, MediatR is widely adopted in the .NET ecosystem with a stable API.

## Implementation Notes

- Feature folder structure: `Features/{FeatureName}/{Commands|Queries}/{OperationName}.cs`
- Each operation file contains: `Request` record, `Handler` class, and optionally a `Validator` class.
- Pipeline behaviors registered in DI order: `ValidationBehavior<,>` → `LoggingBehavior<,>` → Handler.
- Controllers dispatch via `_mediator.Send(request)` and map the result to HTTP responses.

## References

- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Vertical Slice Architecture — Jimmy Bogard](https://www.jimmybogard.com/vertical-slice-architecture/)
- Feature 06: RESTful API — Request Pipeline (Section 5.1)
- Feature 08: Security Hardening — InputValidator / ValidationBehavior
