# ADR-0007: Token Revocation and Temporary Account Lockout

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

Short-lived JWT access tokens reduce exposure but do not by themselves provide immediate revocation when a token is compromised. The back-office also needs stronger protection against slow, distributed password-guessing attacks that can bypass pure IP-based throttling.

## Decision

We will add two server-side security controls to the JWT design:

1. **Token revocation**
   - Access tokens include `jti` and `tokenVersion` claims.
   - The `User` record stores a current `TokenVersion`.
   - Normal logout or incident-driven single-token revocation adds the current `jti` to a deny-list until the token expires.
   - User-wide revocation increments `TokenVersion`, invalidating all previously issued tokens for that user.

2. **Temporary account lockout**
   - Failed login attempts are tracked per normalized email identifier.
   - After **10 failed attempts in 15 minutes**, the account enters a **30-minute lockout** window.
   - Lockout complements, rather than replaces, IP and email rate limits.

## Options Considered

### Option 1: Stateless JWT Only
- **Pros:** Simplest implementation, no server-side revocation state.
- **Cons:** No immediate revocation, weak response to compromised tokens, no durable account lockout.

### Option 2: JWT with Deny-List and User Token Version
- **Pros:** Immediate revocation path, user-wide revocation without per-request session tables, still compatible with short-lived access tokens.
- **Cons:** Requires a lightweight server-side lookup or cache on protected requests.

### Option 3: Full Refresh-Token Session Store
- **Pros:** Richest session-management model, supports rotation and per-device session management.
- **Cons:** More moving parts than needed for the initial release, especially since silent refresh is intentionally deferred.

## Consequences

### Positive
- Compromised tokens can be invalidated before natural expiry.
- Security incidents can revoke all active tokens for a user by incrementing `TokenVersion`.
- Temporary lockout slows distributed brute-force attacks beyond IP-only throttling.

### Negative
- Protected requests are no longer purely self-contained; they require a revocation-state check.
- Authentication state now includes additional user fields and a deny-list cache.

## Implementation Notes

- New token claims: `jti`, `tokenVersion`.
- New user fields: `TokenVersion`, `FailedLoginCount`, `LastFailedLoginAt`, `LockoutEndAt`.
- Deny-list storage:
  - Development / single-instance: in-memory cache.
  - Production: distributed cache (Redis or equivalent).
- Lockout response: `423 Locked` or `401 Unauthorized` with a generic message and optional `Retry-After` header. The public message remains non-enumerating.

## References

- L1-006: Authentication and Authorization
- L2-023: JWT-Based Authentication
- L2-027: Rate Limiting
- Feature 01: Authentication & Authorization
- Feature 08: Security Hardening
