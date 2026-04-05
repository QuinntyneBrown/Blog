# Conformance Log

This file tracks gaps between the detailed design specifications and the actual implementation.

---

## 2026-04-04 — Missing per-email rate limit on login endpoint

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 7.3 — Rate Limiting on Login

**Description:**
The design specifies layered rate limiting on `POST /api/auth/login`: 10 requests per minute per client IP address **and** 5 requests per 15 minutes per normalized email address. The implementation in `Program.cs` only registers the IP-based sliding-window policy (`login-ip`). The per-email rate limit policy (`login-email`) is entirely absent — neither registered in the rate limiter configuration nor enforced in the `AuthController` or login command handler. As a result, a single email account can be hammered indefinitely from different IP addresses, bypassing the email-level protection the design intends.

**Status:** FIXED

---

## 2026-04-04 — LastLoginAt update not persisted to database

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 3.2 — AuthService / Section 5.1 — Login Flow (step 9)

**Description:**
The design specifies that `AuthService` updates `LastLoginAt` on the user record during a successful login (step 9 of the login flow). The `LoginCommandHandler` sets `user.LastLoginAt = DateTime.UtcNow` and calls `userRepository.Update(user)`, but it never injects `IUnitOfWork` or calls `SaveChangesAsync()`. As a result, the `LastLoginAt` timestamp is modified in the in-memory entity but never written to the database. Every other command handler in the codebase (e.g., `CreateArticleCommandHandler`, `DeleteArticleCommandHandler`) correctly injects `IUnitOfWork` and persists changes. The login handler was the sole exception.

**Status:** FIXED
