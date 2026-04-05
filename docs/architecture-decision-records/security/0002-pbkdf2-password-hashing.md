# ADR-0002: PBKDF2 Password Hashing

**Date:** 2026-04-04
**Category:** security
**Status:** Accepted
**Deciders:** Architecture Team

## Context

User passwords must be stored securely. Plaintext or weakly hashed passwords are a critical vulnerability — if the database is compromised, attackers can recover user credentials. The hashing algorithm must be slow enough to resist brute-force attacks but fast enough to not degrade login performance. The requirement specifies a minimum of 100,000 iterations for PBKDF2 (L2-024).

The hash format must encode the algorithm and parameters so that future migration to a stronger algorithm (Argon2, bcrypt) is backward-compatible without requiring all users to reset passwords.

## Decision

We will hash passwords using **PBKDF2-SHA256** with a minimum of **100,000 iterations**, a **128-bit cryptographically random salt** per user, and a **256-bit derived key**. The stored hash format encodes the algorithm, iteration count, salt, and hash to support future algorithm migration.

## Options Considered

### Option 1: PBKDF2-SHA256 (100,000+ iterations)
- **Pros:** Built into .NET via `System.Security.Cryptography.Rfc2898DeriveBytes`, no external dependency, NIST-recommended algorithm, configurable iteration count, well-understood security properties, the .NET implementation handles salt generation and timing-safe comparison.
- **Cons:** Less memory-hard than Argon2 (vulnerable to GPU/ASIC attacks at extreme scale), iteration count must be tuned as hardware improves.

### Option 2: bcrypt
- **Pros:** Designed specifically for password hashing, built-in salt, cost factor is adjustable, widely used in web applications.
- **Cons:** Requires a third-party NuGet package (e.g., BCrypt.Net), 72-byte password length limit (truncates longer passwords), not built into .NET.

### Option 3: Argon2id
- **Pros:** Memory-hard — resistant to GPU/ASIC brute-force attacks, winner of the Password Hashing Competition, considered the current best practice.
- **Cons:** Requires a third-party NuGet package (e.g., Konscious.Security.Cryptography or libsodium binding), less widely deployed in .NET ecosystem, more complex parameter tuning (memory, parallelism, iterations).

## Consequences

### Positive
- Zero external dependencies — uses `System.Security.Cryptography` built into .NET.
- 100,000 iterations makes brute-force attacks computationally expensive (each guess requires ~100ms on modern hardware).
- Per-user salt prevents rainbow table attacks and ensures identical passwords produce different hashes.
- Self-describing hash format (`$pbkdf2-sha256$iterations$salt$hash`) enables transparent migration to Argon2 in the future.
- Timing-safe comparison in `PasswordHasher.VerifyPassword()` prevents timing attacks.

### Negative
- PBKDF2 is not memory-hard — it is more vulnerable than Argon2 to attackers with GPU farms.
- Iteration count must be periodically increased as hardware improves.

### Risks
- If a database breach occurs and the attacker has GPU resources, PBKDF2 is weaker than Argon2. However, 100,000 iterations with per-user salt provides strong protection against realistic attack scenarios for a blog platform. Migration to Argon2 is possible without user disruption thanks to the self-describing hash format.

## Implementation Notes

- `PasswordHasher.HashPassword(string password)` → generates salt, derives key, returns encoded string.
- `PasswordHasher.VerifyPassword(string password, string storedHash)` → parses encoded string, re-derives key, timing-safe comparison.
- Hash format: `$pbkdf2-sha256$100000$<base64-salt>$<base64-hash>`.
- Salt: 128-bit (16 bytes) via `RandomNumberGenerator.Fill()`.
- Derived key: 256-bit (32 bytes).
- Error messages on login failure: generic "Invalid email or password" to prevent user enumeration (L2-024 AC3).
- Passwords are never stored or logged in plaintext.

## References

- [NIST SP 800-132: Password-Based Key Derivation](https://csrc.nist.gov/publications/detail/sp/800-132/final)
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- L2-024: Password Security
- Feature 01: Authentication — PasswordHasher (Section 3.4)
