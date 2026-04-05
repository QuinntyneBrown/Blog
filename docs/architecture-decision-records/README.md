# Architecture Decision Records

This directory contains the Architecture Decision Records (ADRs) for the Blog platform. Each ADR captures a significant architectural decision, the context that motivated it, the options considered, and the consequences of the choice.

ADRs are organized by category:

## Backend (9 ADRs)

| # | Decision | Status |
|---|----------|--------|
| [0001](backend/0001-aspnet-core-8-as-application-framework.md) | Use ASP.NET Core 8 as Application Framework | Accepted |
| [0002](backend/0002-vertical-slice-architecture-with-mediatr.md) | Vertical Slice Architecture with MediatR | Accepted |
| [0003](backend/0003-server-side-rendering-with-razor-pages.md) | Server-Side Rendering with ASP.NET Razor Pages | Accepted |
| [0004](backend/0004-restful-api-with-rfc-7807-problem-details.md) | RESTful API Design with RFC 7807 Problem Details | Accepted |
| [0005](backend/0005-fluentvalidation-in-mediatr-pipeline.md) | FluentValidation in MediatR Pipeline | Accepted |
| [0006](backend/0006-response-compression-brotli-gzip.md) | Response Compression with Brotli and Gzip | Accepted |
| [0007](backend/0007-http-caching-strategy.md) | Layered HTTP Caching Strategy | Accepted |
| [0008](backend/0008-seo-and-discoverability-strategy.md) | Comprehensive SEO and Discoverability Strategy | Accepted |
| [0009](backend/0009-digital-asset-management-with-content-negotiation.md) | Digital Asset Management with Content Negotiation | Accepted |

## Security (6 ADRs)

| # | Decision | Status |
|---|----------|--------|
| [0001](security/0001-jwt-based-authentication.md) | JWT-Based Authentication for Back-Office API | Accepted |
| [0002](security/0002-pbkdf2-password-hashing.md) | PBKDF2 Password Hashing | Accepted |
| [0003](security/0003-html-sanitization-for-xss-prevention.md) | HTML Sanitization with HtmlSanitizer for XSS Prevention | Accepted |
| [0004](security/0004-security-headers-and-https-enforcement.md) | Security Headers and HTTPS Enforcement | Accepted |
| [0005](security/0005-rate-limiting-with-sliding-window.md) | Rate Limiting with Sliding Window | Accepted |
| [0006](security/0006-cors-policy.md) | Strict CORS Policy | Accepted |

## Data (2 ADRs)

| # | Decision | Status |
|---|----------|--------|
| [0001](data/0001-ef-core-code-first-with-repository-pattern.md) | EF Core Code-First with Repository and Unit of Work Patterns | Accepted |
| [0002](data/0002-forward-only-database-migrations.md) | Forward-Only Database Migrations with EF Core | Accepted |

## Frontend (2 ADRs)

| # | Decision | Status |
|---|----------|--------|
| [0001](frontend/0001-minimal-javascript-progressive-enhancement.md) | Minimal JavaScript with Progressive Enhancement | Accepted |
| [0002](frontend/0002-responsive-five-breakpoint-layout.md) | Responsive Five-Breakpoint Layout System | Accepted |

## Infrastructure (1 ADR)

| # | Decision | Status |
|---|----------|--------|
| [0001](infrastructure/0001-structured-json-logging-with-serilog.md) | Structured JSON Logging with Serilog | Accepted |

## ADR Format

Each ADR follows this structure:

1. **Context** — What situation motivates this decision?
2. **Decision** — What are we doing?
3. **Options Considered** — What alternatives were evaluated (with honest pros/cons)?
4. **Consequences** — What are the positive, negative, and risk trade-offs?
5. **Implementation Notes** — How should this be implemented?
6. **References** — Links to specs, requirements, and related ADRs.

## Requirements Traceability

All ADRs trace back to the L1 (high-level) and L2 (detailed) requirements in `docs/specs/`. The traceability ensures every architectural decision is motivated by a documented requirement.
