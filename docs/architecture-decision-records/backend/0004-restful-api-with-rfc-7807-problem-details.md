# ADR-0004: RESTful API Design with RFC 7807 Problem Details

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform's API supports the Razor Pages-based back-office administration UI and the public-facing web experience wherever a programmatic HTTP surface is needed. These consumers need a predictable API surface with consistent URL conventions, HTTP semantics, response envelopes, and error formats. The API must handle validation errors, authentication failures, rate limiting, and business rule violations in a standardized way.

Custom error formats create friction for API consumers who must learn bespoke schemas. Industry standards exist for API error reporting.

## Decision

We will design the API following **RESTful conventions** with:
- **Plural nouns** for resource collections (`/api/articles`, `/api/digital-assets`).
- **Standard HTTP methods** with correct semantics (GET reads, POST creates, PUT replaces, DELETE removes).
- **RFC 7807 Problem Details** for all error responses.
- **Uniform response envelopes** wrapping successful responses in `{ data: T, timestamp: DateTime }`.
- **Offset-based pagination** with metadata (totalCount, totalPages, navigation URLs).

## Options Considered

### Option 1: RESTful API with RFC 7807 Problem Details
- **Pros:** RFC 7807 is an IETF standard understood by HTTP tooling and client libraries, consistent error schema across all endpoints, ASP.NET Core has built-in `ProblemDetails` support, response envelopes provide predictable parsing, plural nouns and standard methods follow established conventions.
- **Cons:** Response envelopes add a wrapping layer that some purists consider unnecessary, RFC 7807 requires discipline to map all exception types correctly.

### Option 2: GraphQL
- **Pros:** Flexible queries, clients request exactly the data they need, single endpoint.
- **Cons:** Overkill for a blog with three entity types, GraphQL error handling is less standardized, caching is more complex (no native HTTP caching), additional tooling required (schema definition, resolvers).

### Option 3: Custom Error Format
- **Pros:** Can be tailored exactly to the application's needs.
- **Cons:** No industry standard recognition, every consumer must learn the custom format, no tooling support, reinvents what RFC 7807 already provides.

## Consequences

### Positive
- Error responses are machine-readable and consistent: `type`, `title`, `status`, `detail`, and optional `errors` dictionary for validation failures.
- ASP.NET Core's `ProblemDetailsFactory` and `[ApiController]` attribute provide built-in support.
- Response envelope middleware wraps all 2xx responses automatically.
- Pagination metadata (totalCount, hasNextPage, nextPageUrl) enables efficient client-side navigation.
- Status code mapping is explicit: 200/201/204 for success, 400/401/404/409/413/429/500 for errors.

### Negative
- The response envelope adds a layer of indirection — raw data is nested under `data`.
- Exception-to-ProblemDetails mapping must be maintained in the `ExceptionHandlingMiddleware`.

### Risks
- If API versioning is needed later (e.g., `/api/v2/articles`), it was not introduced from the start. This is a conscious deferral — versioning will be added when a breaking change is required.

## Implementation Notes

- `ApiController` base class applies `[ApiController]` and `[Route("api/[controller]")]`.
- `ExceptionHandlingMiddleware` catches all unhandled exceptions and maps to ProblemDetails: `ValidationException` → 400, `NotFoundException` → 404, `ConflictException` → 409, `FileTooLargeException` → 413, `RateLimitExceededException` → 429, all others → 500.
- `ResponseEnvelopeMiddleware` wraps 2xx responses in `ApiResponse<T>`. Skips streaming responses and health checks.
- Pagination: page size min 1, max 100, default 20. Parameters via query string: `?page=1&pageSize=20`.

## References

- [RFC 7807 — Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807)
- L2-030: RESTful API Conventions
- L2-031: API Pagination
- Feature 06: RESTful API — Full design
