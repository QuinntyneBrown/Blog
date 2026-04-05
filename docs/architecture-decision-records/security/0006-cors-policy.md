# ADR-0006: Strict CORS Policy

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog API is consumed by a back-office SPA running on a potentially different origin (e.g., the SPA is served from `https://admin.blog.example.com` while the API is at `https://api.blog.example.com`). Browsers enforce the Same-Origin Policy, which blocks cross-origin requests unless the server explicitly permits them via CORS headers. A permissive CORS policy (`Access-Control-Allow-Origin: *`) would allow any website to make authenticated requests to the API on behalf of a logged-in user (L2-042).

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
- **Cons:** Prevents the SPA from being hosted on a different origin than the API, limits deployment flexibility.

## Consequences

### Positive
- Cross-origin requests are only accepted from trusted, configured origins.
- Browser-enforced policy — no server-side trust required.
- Preflight caching (7200s) reduces roundtrips for SPA clients.
- Configuration-driven — adding a new origin requires only an `appsettings.json` change.

### Negative
- Origin list must be kept in sync with actual client deployment URLs.
- Development environments need `https://localhost:*` entries for local SPA development.

### Risks
- Misconfigured origins could block the SPA from functioning. Staging environment testing should verify CORS behavior before production deployment.

## Implementation Notes

- Configuration: `Cors:AllowedOrigins` array in `appsettings.json`.
- Allowed methods: `GET, POST, PUT, PATCH, DELETE, OPTIONS`.
- Allowed headers: `Authorization, Content-Type`.
- `AllowCredentials = true` for configured origins (required for JWT in `Authorization` header).
- Preflight max age: 7200 seconds (2 hours).
- Middleware positioned after rate limiting, before authentication.

## References

- [CORS — MDN](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [OWASP CORS Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/CORS_OriginAccessControl_Cheat_Sheet.html)
- L2-042: CORS Policy
- Feature 08: Security Hardening — CorsMiddleware (Section 3.4)
