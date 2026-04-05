# ADR-0001: JWT-Based Authentication for Back-Office API

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform has two distinct access patterns: a public site that is entirely anonymous (no authentication) and a back-office Razor Pages administration interface that requires authentication for all administrative operations (L1-006). The authentication mechanism must protect content management, user management, and digital asset operations while keeping protected API calls bearer-token based.

The back-office Razor Pages application communicates with the API server over HTTPS. The authentication mechanism must work well with server-rendered admin pages that still need to invoke protected API endpoints for administrative operations.

## Decision

We will authenticate back-office users via **short-lived JSON Web Tokens (JWT)**. Tokens are issued by the API server upon successful email/password authentication and validated on every request to protected endpoints. The Razor Pages administration application retains the token only within the authenticated admin session and uses it for subsequent protected API requests. The initial release does **not** implement silent refresh tokens; expired admin sessions must re-authenticate.

## Options Considered

### Option 1: JWT Bearer Tokens
- **Pros:** Stateless — no server-side session store required, scales horizontally without shared state, self-contained claims (sub, email, displayName, iat, exp) avoid database lookups on every request, standard `Authorization: Bearer` header works with all HTTP clients, ASP.NET Core has built-in JWT validation via `AddAuthentication().AddJwtBearer()`.
- **Cons:** Tokens cannot be revoked before expiration without additional infrastructure (blacklist), token payload is base64-encoded (not encrypted) — claims are visible to the client, token must be stored securely client-side.

### Option 2: Cookie-Based Sessions
- **Pros:** Automatic browser handling (cookies sent on every request), server-side session allows immediate revocation, well-understood pattern.
- **Cons:** Requires server-side session store (in-memory or Redis), CSRF protection is mandatory (cookies are sent automatically by browsers), does not scale horizontally without shared session store, and is less suitable for APIs that are explicitly bearer-token-oriented.

### Option 3: OAuth 2.0 / OpenID Connect with External Provider
- **Pros:** Delegates credential management to a trusted identity provider, supports SSO, MFA typically included.
- **Cons:** External dependency (identity provider must be available), more complex setup for a single-application blog, overkill when there is one admin user, adds latency for token validation against external provider.

## Consequences

### Positive
- Stateless authentication — no session store to manage or replicate.
- Horizontal scaling is trivial — any server instance can validate the token independently.
- Claims are embedded in the token, reducing database queries on authenticated requests.
- Standard `[Authorize]` attribute on controllers enforces authentication.
- Token expiration is enforced automatically by `JwtMiddleware` on every request.

### Negative
- Tokens cannot be immediately revoked — compromised tokens remain valid until expiration (mitigated by short expiration, default 60 minutes).
- The back-office web application must manage the token across requests without exposing it through browser-local JavaScript storage.
- JWT payload is not encrypted — sensitive data should not be placed in claims.

### Risks
- If uninterrupted long-running admin sessions are needed later, rotating refresh tokens with server-side revocation can be added as a follow-on enhancement. The initial release intentionally avoids a partial refresh design.

## Implementation Notes

- Login endpoint: `POST /api/auth/login` with `{ email, password }` → returns `{ token, expiresAt }`.
- Token claims: `sub` (user ID), `email`, `displayName`, `iat`, `exp`.
- JWT signing: HMAC-SHA256 with a key of at least 256 bits, configured via environment variables.
- Validation: signature, expiration, issuer, audience checked on every request by `JwtMiddleware`.
- Token retained by the Razor Pages administration application as part of the authenticated admin session rather than browser-local JavaScript storage.
- Expired tokens are not silently refreshed in v1; the admin session re-authenticates through the login flow.

## References

- [JWT Specification (RFC 7519)](https://tools.ietf.org/html/rfc7519)
- [ASP.NET Core JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- L1-006: Authentication and Authorization
- L2-023: JWT-Based Authentication
- Feature 01: Authentication & Authorization — Full design
