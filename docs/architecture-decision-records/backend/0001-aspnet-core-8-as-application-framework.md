# ADR-0001: Use ASP.NET Core 8 as Application Framework

**Date:** 2026-04-04
**Category:** backend
**Status:** Accepted
**Deciders:** Architecture Team

## Context

The blog platform requires a server-side framework that can deliver server-rendered HTML pages with sub-200ms TTFB at P95, host a RESTful API for back-office operations, support middleware pipelines for security and caching, and provide a robust dependency injection system. The framework must support JWT authentication, Entity Framework Core for data access, and response compression out of the box.

The platform has two distinct consumers: a public-facing site requiring server-side rendering with minimal JavaScript, and a back-office SPA requiring a secure API. Both are served from the same application.

## Decision

We will use **ASP.NET Core 8 with C#** as the application framework, leveraging Razor Pages for server-side rendering of the public site and Web API controllers for the RESTful back-office API.

## Options Considered

### Option 1: ASP.NET Core 8 (C#)
- **Pros:** Kestrel is one of the fastest HTTP servers available, built-in DI container, mature middleware pipeline architecture, Razor Pages for SSR without a JS framework, native support for EF Core / JWT / response compression / rate limiting, strong typing with C#, long-term support release.
- **Cons:** Smaller open-source ecosystem compared to Node.js, Windows-centric developer tooling perception (though fully cross-platform), steeper learning curve for teams without .NET experience.

### Option 2: Node.js with Express/Fastify
- **Pros:** Largest package ecosystem (npm), JavaScript/TypeScript end-to-end, strong SSR frameworks (Next.js), very large community.
- **Cons:** Single-threaded event loop requires careful handling for CPU-bound work (image processing), TypeScript adds type safety but is optional, no built-in DI or middleware standardization equivalent to ASP.NET Core, ORM ecosystem less mature than EF Core for relational databases.

### Option 3: Django (Python)
- **Pros:** Batteries-included framework with ORM, admin panel, and auth, excellent for rapid prototyping, strong template engine.
- **Cons:** Python's GIL limits true parallelism, slower raw throughput than .NET or Node.js, the built-in template engine is less suited for the responsive component patterns required, Django REST Framework is an add-on.

### Option 4: Go with standard library / Gin
- **Pros:** Exceptional raw performance, compiled binary with minimal dependencies, excellent concurrency model.
- **Cons:** No built-in template engine comparable to Razor Pages, no ORM equivalent to EF Core (manual SQL or lightweight query builders), minimal middleware ecosystem, more boilerplate for common web patterns (validation, auth, error handling).

## Consequences

### Positive
- Kestrel's throughput makes the sub-200ms TTFB target achievable without a reverse proxy cache.
- Razor Pages delivers complete HTML without any client-side framework, meeting the minimal-JavaScript requirement (L2-017).
- The middleware pipeline architecture maps directly to the security pipeline design (Feature 08).
- Built-in rate limiting, response compression, and health checks reduce external dependencies.
- EF Core integration is first-class with code-first migrations and LINQ query translation.

### Negative
- The team must be proficient in C# and the .NET ecosystem.
- Deployment requires the .NET runtime (mitigated by containerization).
- The Angular back-office SPA introduces a second language (TypeScript) alongside C#.

### Risks
- .NET 8 is an LTS release (supported until November 2026). Migration to .NET 9 or later will be required eventually, though ASP.NET Core has a strong track record of backward compatibility.

## Implementation Notes

- Target framework: `net8.0` in all `.csproj` files.
- Use Razor Pages (`AddRazorPages()`) for the public site and API controllers (`AddControllers()`) for the REST API.
- Register services via the built-in DI container in `Program.cs`.
- Middleware pipeline order is critical for correctness — see Feature 07 (Web Performance) and Feature 08 (Security Hardening) designs.

## References

- [ASP.NET Core 8 Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Kestrel Performance Benchmarks](https://www.techempower.com/benchmarks/)
- L1-001 through L1-012 (all high-level requirements)
- Feature 07: Web Performance — Middleware Pipeline Order
