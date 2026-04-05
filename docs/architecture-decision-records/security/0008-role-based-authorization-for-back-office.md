# ADR-0008: Role-Based Authorization for Back-Office Access

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

Authenticated admin access alone is too coarse. A compromised account should not automatically receive unrestricted authority over all operational and security-sensitive actions.

## Decision

We will implement **role-based authorization** with two roles in the initial release:

- **Administrator**
  - Full access to all back-office and operational functions.
  - Can manage users, revoke tokens, review security settings, and perform destructive administrative actions.

- **Editor**
  - Can create, edit, publish, unpublish, and delete articles.
  - Can upload and manage digital assets.
  - Cannot manage users, security settings, or operational configuration.

The `User` entity stores a single `Role` value. JWTs include a `role` claim, and endpoints use `[Authorize(Roles = ...)]` or equivalent policy-based authorization.

## Options Considered

### Option 1: Single Admin Role for All Users
- **Pros:** Simplest design.
- **Cons:** Violates least privilege and expands blast radius of compromise.

### Option 2: Small Fixed Role Set
- **Pros:** Meets least-privilege needs without introducing a full RBAC matrix.
- **Cons:** Less flexible than fine-grained permissions.

### Option 3: Fine-Grained Permission Matrix
- **Pros:** Maximum flexibility.
- **Cons:** Overkill for the initial scope and significantly more complex to operate.

## Consequences

### Positive
- Clear least-privilege boundary for editorial versus administrative actions.
- Authorization rules remain understandable and easy to test.

### Negative
- Some future features may require a third role or permission flags.

## Implementation Notes

- Add `Role` to the `User` entity as an enum/string-backed field.
- Include `role` in JWT claims.
- Default bootstrap user role: `Administrator`.
- Public routes remain anonymous and are not role-gated.

## References

- L1-007: Least Privilege
- Feature 01: Authentication & Authorization
- Feature 08: Security Hardening
