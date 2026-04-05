# ADR-0005: Rate Limiting with Sliding Window

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform must protect authentication endpoints from brute-force attacks and write endpoints from abuse (L2-027). Without rate limiting, an attacker could attempt unlimited password guesses or flood the API with write requests, degrading service for legitimate users.

Rate limiting must be granular: authentication endpoints should be limited per client IP and per normalized email address (to slow credential stuffing and targeted password guessing), while write endpoints should be limited per authenticated user (to prevent abuse by a compromised account).

## Decision

We will enforce **rate limiting** using ASP.NET Core's built-in `System.Threading.RateLimiting` with **sliding window** policies:

| Policy | Scope | Limit | Window |
|--------|-------|-------|--------|
| Authentication endpoints (`/api/auth/*`) | Per client IP address | 10 requests | 1 minute |
| Authentication endpoints (`/api/auth/*`) | Per normalized email address | 5 requests | 15 minutes |
| Write endpoints (POST, PUT, PATCH, DELETE) | Per authenticated user | 60 requests | 1 minute |

When either applicable limit is exceeded, the server returns **429 Too Many Requests** with a `Retry-After` header indicating seconds until the window resets.

## Options Considered

### Option 1: ASP.NET Core Built-In Rate Limiting (Sliding Window)
- **Pros:** Built into .NET 7+, no external dependency, supports multiple policies (fixed window, sliding window, token bucket, concurrency), configurable per-endpoint via attributes or conventions, in-memory counter store is sufficient for single-instance deployment.
- **Cons:** In-memory counters do not share across instances (requires Redis or similar for horizontal scaling), sliding window is an approximation (not perfectly smooth).

### Option 2: Token Bucket Rate Limiting
- **Pros:** Smooths out burst traffic, allows short bursts up to the bucket capacity.
- **Cons:** More complex to reason about (bucket size + refill rate), less intuitive limit specification than "N requests per M seconds".

### Option 3: External Rate Limiting (API Gateway / Reverse Proxy)
- **Pros:** Centralized, language-agnostic, handles rate limiting before request reaches the application.
- **Cons:** Adds infrastructure dependency, less granular (harder to rate-limit per authenticated user without custom logic), not available in development environment.

### Option 4: No Rate Limiting
- **Pros:** Simplest implementation.
- **Cons:** Open to brute-force attacks on authentication, open to API abuse, fails security audits.

## Consequences

### Positive
- Brute-force password attacks are throttled both per IP and per target account identifier.
- Write endpoint abuse is limited to 60 requests per minute per user.
- `Retry-After` header tells legitimate clients when to retry, enabling graceful degradation.
- Sliding window prevents the "boundary burst" problem of fixed windows (where 2x requests occur at the window boundary).
- Rate limit violations are logged for security monitoring.

### Negative
- In-memory counters are per-instance — scaling to multiple instances requires a distributed counter store (Redis).
- Shared IP addresses (NAT, corporate networks) may cause legitimate users to be rate-limited together on auth endpoints.
- Email-based limits must normalize input (trim + lowercase) to avoid trivial bypasses.

### Risks
- If the platform scales horizontally, rate limit counters must be moved to Redis or another distributed store. This is a known migration path documented in Feature 08 open questions.

## Implementation Notes

- Policies registered in `Program.cs` via `AddRateLimiter()` with named policies.
- Auth IP policy: `SlidingWindowRateLimiter` with `PermitLimit = 10, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6`.
- Auth email policy: `SlidingWindowRateLimiter` with `PermitLimit = 5, Window = TimeSpan.FromMinutes(15), SegmentsPerWindow = 5`.
- Write policy: `SlidingWindowRateLimiter` with `PermitLimit = 60, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6`.
- IP extraction: `HttpContext.Connection.RemoteIpAddress` (respects `X-Forwarded-For` behind reverse proxy).
- Email extraction: normalized login identifier from the deserialized login request body.
- User extraction: `HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)`.
- 429 response includes `Retry-After` header with seconds remaining.

## References

- [ASP.NET Core Rate Limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [OWASP Blocking Brute Force Attacks](https://owasp.org/www-community/controls/Blocking_Brute_Force_Attacks)
- L2-027: Rate Limiting
- Feature 08: Security Hardening — RateLimitingMiddleware (Section 3.3)
