# ADR-0010: OpenAPI Contract Generation with Swashbuckle

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The API documentation currently exists primarily as narrative design docs. The platform needs a machine-readable contract that stays in sync with implemented controllers and supports automated validation.

## Decision

We will generate an **OpenAPI 3.0** document from the ASP.NET Core API using **Swashbuckle**.

- Swagger/OpenAPI JSON is produced from controller/action metadata.
- Swagger UI is enabled in development and staging.
- CI exports the OpenAPI document as a build artifact and validates that it can be generated successfully.

## Options Considered

### Option 1: Manual API Documentation Only
- **Pros:** No tooling required.
- **Cons:** Drifts from implementation and is not machine-readable.

### Option 2: Generated OpenAPI with Swashbuckle
- **Pros:** Native ASP.NET Core support, easy adoption, contract stays close to code, usable by client tooling.
- **Cons:** Requires attribute discipline and example curation.

### Option 3: Code-First External Contract Tooling
- **Pros:** Strong contract governance.
- **Cons:** More complexity than needed for the initial release.

## Consequences

### Positive
- API contracts can be validated automatically.
- Consumers get a reliable machine-readable schema.
- Future client SDK generation becomes straightforward.

### Negative
- Example payloads and schema annotations still require maintenance.

## Implementation Notes

- Generate one canonical spec for the admin API surface.
- Exclude internal-only operational routes from public Swagger UI when appropriate.
- Keep successful-response envelope and RFC 7807 schemas explicitly modeled.

## References

- Feature 06: RESTful API
- L2-030: RESTful API Conventions
- L2-031: API Pagination
