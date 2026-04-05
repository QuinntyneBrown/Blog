# Conformance Log

This file tracks gaps between the detailed design specifications and the actual implementation.

---

## 2026-04-04 — Missing per-email rate limit on login endpoint

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 7.3 — Rate Limiting on Login

**Description:**
The design specifies layered rate limiting on `POST /api/auth/login`: 10 requests per minute per client IP address **and** 5 requests per 15 minutes per normalized email address. The implementation in `Program.cs` only registers the IP-based sliding-window policy (`login-ip`). The per-email rate limit policy (`login-email`) is entirely absent — neither registered in the rate limiter configuration nor enforced in the `AuthController` or login command handler. As a result, a single email account can be hammered indefinitely from different IP addresses, bypassing the email-level protection the design intends.

**Status:** FIXED

---

## 2026-04-04 — Per-email rate limit: interface declared but never implemented, registered, or enforced

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 7.3 — Rate Limiting on Login

**Description:**
The previous conformance entry marked the per-email rate limit as FIXED after `IEmailRateLimitService` was introduced, but the fix was incomplete. The concrete implementation (`EmailRateLimitService`) did not exist, the service was never registered in `Program.cs`, `LoginCommandHandler` did not inject or call it, and `TooManyRequestsException` (the appropriate HTTP-429 exception type) was missing entirely. As a result, the per-email sliding-window policy remained entirely unenforced at runtime despite the interface declaration. A single email address could still be brute-forced from unlimited IP addresses, violating the 5-attempts-per-15-minutes-per-email guarantee stated in the design.

**Fix applied:**
- Created `src/Blog.Api/Services/EmailRateLimitService.cs` — in-memory sliding-window implementation (5 attempts / 15-minute window, thread-safe via `ConcurrentDictionary` + `lock`).
- Registered `IEmailRateLimitService` → `EmailRateLimitService` as a singleton in `Program.cs`.
- Created `src/Blog.Api/Common/Exceptions/TooManyRequestsException.cs` and added the 429 case to `ExceptionHandlingMiddleware`.
- Injected `IEmailRateLimitService` into `LoginCommandHandler` and called `TryAcquire` before any database access; throws `TooManyRequestsException` when the limit is exceeded.

**Status:** FIXED

---

## 2026-04-04 — LastLoginAt update not persisted to database

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 3.2 — AuthService / Section 5.1 — Login Flow (step 9)

**Description:**
The design specifies that `AuthService` updates `LastLoginAt` on the user record during a successful login (step 9 of the login flow). The `LoginCommandHandler` sets `user.LastLoginAt = DateTime.UtcNow` and calls `userRepository.Update(user)`, but it never injects `IUnitOfWork` or calls `SaveChangesAsync()`. As a result, the `LastLoginAt` timestamp is modified in the in-memory entity but never written to the database. Every other command handler in the codebase (e.g., `CreateArticleCommandHandler`, `DeleteArticleCommandHandler`) correctly injects `IUnitOfWork` and persists changes. The login handler was the sole exception.

**Status:** FIXED
