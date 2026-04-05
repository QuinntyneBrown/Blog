# ADR-0004: Security Headers and HTTPS Enforcement

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform must be hardened against the OWASP Top 10 (L1-007). Several attack categories (protocol downgrade, clickjacking, MIME-type confusion, XSS via inline scripts) are mitigated by HTTP response headers that instruct browsers to enforce security policies. The platform must serve all traffic over HTTPS and emit a comprehensive set of security headers on every response (L2-026).

These headers form a defense-in-depth layer — even if application-level defenses have a gap, browser-enforced policies limit the damage.

## Decision

We will enforce **HTTPS-only communication** via `HttpsRedirectionMiddleware` (301 redirect from HTTP to HTTPS) and emit the following **security headers** on every response via a custom `SecurityHeadersMiddleware`:

| Header | Value |
|--------|-------|
| Strict-Transport-Security | `max-age=31536000; includeSubDomains; preload` |
| Content-Security-Policy | `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'` |
| X-Content-Type-Options | `nosniff` |
| X-Frame-Options | `DENY` |
| Referrer-Policy | `strict-origin-when-cross-origin` |
| Permissions-Policy | `camera=(), microphone=(), geolocation=(), payment=()` |

The `Server` header is removed to prevent information disclosure.

## Options Considered

### Option 1: Application-Level Security Headers Middleware
- **Pros:** Headers applied regardless of reverse proxy configuration, consistent across all environments (dev, staging, prod), configurable per-environment via `SecurityHeadersConfig`, middleware runs early in pipeline so headers are present even on error responses.
- **Cons:** Must be maintained in application code, CSP requires careful tuning to avoid breaking legitimate functionality.

### Option 2: Reverse Proxy / CDN Headers (Nginx, Cloudflare)
- **Pros:** Applied at the infrastructure level, centralized configuration.
- **Cons:** CDN is deferred, headers missing in direct-to-origin scenarios, configuration drift between environments, not visible in the application codebase.

### Option 3: No Security Headers (Rely on Application Logic Only)
- **Pros:** Simplest configuration.
- **Cons:** Missing defense-in-depth, browsers cannot enforce security policies, fails security audits.

## Consequences

### Positive
- HSTS with preload ensures browsers never make HTTP requests to the domain after first visit.
- CSP blocks inline script execution (primary XSS vector), restricts resource loading to same origin.
- `X-Frame-Options: DENY` prevents clickjacking by blocking iframe embedding.
- `nosniff` prevents MIME-type confusion attacks.
- `Permissions-Policy` disables browser features the blog does not use, reducing attack surface from compromised third-party scripts.
- Removing the `Server` header prevents technology fingerprinting.

### Negative
- `style-src 'unsafe-inline'` is a pragmatic concession — critical CSS inlining (Feature 07) requires inline `<style>` blocks. This weakens CSP for styles but does not affect script-src.
- CSP must be updated if third-party resources (fonts CDN, analytics) are added.

### Risks
- Overly restrictive CSP can break legitimate functionality. Testing in staging is essential before production deployment. CSP report-uri can be added to monitor violations before enforcing.

## Implementation Notes

- `SecurityHeadersMiddleware` runs early in the pipeline (after HTTPS redirect, before routing).
- Headers are configured via `SecurityHeadersConfig` class bound from `appsettings.json`.
- Server header removed via Kestrel configuration: `options.AddServerHeader = false`.
- CSP could be strengthened in the future by replacing `'unsafe-inline'` with nonce-based style injection (see Feature 08 open questions).

## References

- [OWASP Secure Headers Project](https://owasp.org/www-project-secure-headers/)
- [Content Security Policy (MDN)](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
- [HSTS Preload](https://hstspreload.org/)
- L2-026: HTTPS and Security Headers
- Feature 08: Security Hardening — SecurityHeadersMiddleware (Section 3.2)
