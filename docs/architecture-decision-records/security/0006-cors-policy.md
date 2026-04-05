# ADR-0006: Strict CORS Policy

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog API is primarily consumed by first-party Razor Pages experiences served from the same ASP.NET Core application boundary. Same-origin requests do not require CORS, but any separately hosted admin tools, staging frontends, or future integrations would. Browsers enforce the Same-Origin Policy, which blocks cross-origin requests unless the server explicitly permits them via CORS headers. A permissive CORS policy (`Access-Control-Allow-Origin: *`) would allow any website to call browser-accessible API endpoints from an arbitrary origin (L2-042).

The public-facing SSR pages are same-origin with the API (served from the same ASP.NET Core process), so they do not trigger CORS.

## Decision

We will enforce a **strict CORS policy** where only explicitly configured origins are allowed. Allowed origins are loaded from `appsettings.json` under `Cors:AllowedOrigins`. Requests from unlisted origins receive no CORS headers, causing the browser to block the response. Preflight (OPTIONS) requests are handled correctly with appropriate `Access-Control-Allow-*` headers.

## Options Considered

### Option 1: Explicit Origin Allow-List
- **Pros:** Only trusted origins can make cross-origin requests, credentials are only shared with known origins, preflight responses are cached (7200s) to reduce OPTIONS requests, configuration-driven (no code change to add origins).
- **Cons:** Must be updated when new client origins are added, misconfiguration can block legitimate clients.

### Option 2: Wildcard CORS (`Access-Control-Allow-Origin: *`)
- **Pros:** Simplest configuration, works with any client.
- **Cons:** Any website can make requests to the API, cannot be combined with `Access-Control-Allow-Credentials: true`, major security vulnerability for authenticated endpoints.

### Option 3: No CORS (Same-Origin Only)
- **Pros:** Most restrictive, eliminates cross-origin attack surface.
- **Cons:** Prevents any separately hosted admin tools or approved external browser clients from calling the API, limiting deployment flexibility.

## Consequences

### Positive
- Cross-origin requests are only accepted from trusted, configured origins.
- Browser-enforced policy — no server-side trust required.
- Preflight caching (7200s) reduces roundtrips for approved browser-based clients.
- Configuration-driven — adding a new origin requires only an `appsettings.json` change.

### Negative
- Origin list must be kept in sync with actual client deployment URLs.
- Development environments need explicit `https://localhost:*` entries when testing approved cross-origin browser clients against the API.

### Risks
- Misconfigured origins could block approved cross-origin clients. Staging environment testing should verify CORS behavior before production deployment.

## Implementation Notes

- Configuration: `Cors:AllowedOrigins` array in `appsettings.json`.
- Allowed methods: `GET, POST, PUT, PATCH, DELETE, OPTIONS`.
- Allowed headers: `Authorization, Content-Type`.
- `AllowCredentials` remains disabled unless a specific cross-origin cookie-based flow requires it; bearer tokens sent in the `Authorization` header do not require credentialed CORS.
- Preflight max age: 7200 seconds (2 hours).
- Middleware positioned after rate limiting, before authentication.

## References

- [CORS — MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [OWASP CORS Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/CORS_OriginAccessControl_Cheat_Sheet.html)
- L2-042: CORS Policy
- Feature 08: Security Hardening — CorsMiddleware (Section 3.4)
