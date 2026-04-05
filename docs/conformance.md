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

---

## 2026-04-04 — Article Version not incremented on update, publish, or delete

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 5.2 (step 7), Section 5.3 (step 5), Section 5.4 (step 3)

**Description:**
The design specifies that the `Version` concurrency token is incremented on every mutation — update (Section 5.2, step 7: "increments `Version`"), publish/unpublish (Section 5.3, step 5: "Increments `Version`, persists, and returns 200 with a fresh `ETag`"), and delete (Section 5.4, step 3). The `UpdateArticleCommandHandler`, `PublishArticleCommandHandler`, and `DeleteArticleCommandHandler` all validate the incoming `If-Match` header against the current version but never call `article.Version++` before persisting. This means the ETag returned after an update is identical to the one sent with the request, effectively breaking optimistic concurrency — a second concurrent update with the same stale ETag would succeed instead of returning 412.

**Status:** FIXED

---

## 2026-04-04 — Missing WCAG accessibility landmarks and skip-to-content link in public layout

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 7.1 — Semantic Markup and ARIA Landmarks, Section 7.4 — Keyboard Navigation

**Description:**
The design specifies a complete set of ARIA landmarks and a skip-to-content link for WCAG 2.1 Level AA compliance. The `_Layout.cshtml` was missing all of the following:
1. **Skip-to-content link** (Section 7.4: "provided as the first focusable element to bypass navigation") — completely absent.
2. **`<header role="banner">`** (Section 7.1) — the `<nav>` sat directly in `<body>` with no `<header>` wrapper.
3. **`<nav aria-label="Main navigation">`** (Section 7.1) — the `<nav>` had no `aria-label`.
4. **`<main role="main">`** (Section 7.1) — the `<main>` element had no `role` attribute.
5. **`<footer role="contentinfo">`** (Section 7.1) — the `<footer>` element had no `role` attribute.
6. **Hamburger button `aria-controls`** (Section 7.4) — the hamburger `<button>` had `aria-expanded` but was missing the required `aria-controls="mobile-menu"` attribute linking it to the menu panel.

**Status:** FIXED

---

## 2026-04-04 — Missing unique index on DigitalAsset.StoredFileName

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 4.1 — DigitalAsset Entity

**Description:**
The design specifies that `StoredFileName` must be "required, unique, max 256 chars" on the `DigitalAsset` entity. The EF Core configuration in `DigitalAssetConfiguration.cs` applied `.IsRequired().HasMaxLength(256)` but did not define a unique index on `StoredFileName`. Other entities in the codebase follow this pattern correctly — `Article.Slug` and `User.Email` both have `.HasIndex(...).IsUnique()`. Without the unique constraint, the database would silently allow duplicate stored filenames, which could cause one asset's file to shadow another during serving via `GET /assets/{filename}`.

**Status:** FIXED

---

## 2026-04-04 — DigitalAsset Width and Height are nullable instead of required

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 4.1 — DigitalAsset Entity

**Description:**
The design specifies `Width` (int, Required) and `Height` (int, Required) — both are set after image processing during upload (Section 5.1, step 8). The implementation declared both fields as `int?` (nullable) in `DigitalAsset.cs`, used `int?` in `DigitalAssetDto`, and the upload handler initialized them as `null` with a silent `catch {}` block swallowing any image-loading failure. Since the upload endpoint only accepts validated image content types (JPEG, PNG, WebP, AVIF), dimensions should always be extractable. A silent failure here would persist an asset with null dimensions, violating the data model contract and breaking any downstream code that relies on width/height for responsive image rendering.

**Fix applied:**
- Changed `DigitalAsset.Width` and `DigitalAsset.Height` from `int?` to `int`.
- Changed `DigitalAssetDto` fields from `int?` to `int`.
- Removed the silent `try/catch` in `UploadDigitalAssetCommandHandler` — dimension extraction now propagates exceptions if it fails on a validated image type.
- Updated `DigitalAssetTests` to assert default `0` instead of `null`.

**Status:** FIXED

---

## 2026-04-04 — Digital asset deletion allows deleting assets still referenced by articles

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 8 — Open Question #6

**Description:**
The design resolves Open Question #6 with: "Hard delete with orphan protection. Deletion is allowed only for assets not referenced by any article's `FeaturedImageId`. The API returns 409 Conflict if the asset is in use." The `DeleteDigitalAssetCommandHandler` performed no referential integrity check — it immediately deleted the file from disk and removed the entity from the database regardless of whether any articles still referenced the asset via `FeaturedImageId`. This could leave articles pointing to a non-existent featured image, resulting in broken images on both the public site and the back-office editor.

**Fix applied:**
- Added `AnyByFeaturedImageIdAsync(Guid digitalAssetId)` to `IArticleRepository` and `ArticleRepository`.
- Added a pre-deletion check in `DeleteDigitalAssetCommandHandler` that queries for any articles referencing the asset and throws `ConflictException` (409) if found.

**Status:** FIXED

---

## 2026-04-04 — Missing Vary: Accept header on asset serving endpoint

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 5.2 (step 7) and Section 6.3 — GET /assets/{filename}

**Description:**
The design specifies that the asset serving endpoint must set `Vary: Accept` on responses to "indicate content-negotiated responses" and ensure caches distinguish between format-negotiated variants (Section 7.3: "The `Vary: Accept` header ensures caches distinguish between format-negotiated responses"). The `AssetsController.Serve` method set `Cache-Control` and `ETag` headers but omitted `Vary: Accept`. Without this header, a CDN or browser cache could serve a JPEG response to a client that supports AVIF/WebP, or vice versa, once content negotiation is implemented.

**Status:** FIXED

---

## 2026-04-04 — ResponseEnvelopeMiddleware missing; controllers manually wrap responses instead

**Design reference:** `docs/detailed-designs/06-restful-api/README.md`, Section 3.4 — ResponseEnvelopeMiddleware; Open Question 1 (resolved: opt-out via `[RawResponse]`)

**Description:**
The design specifies a dedicated `ResponseEnvelopeMiddleware` that intercepts all 2xx JSON API responses and automatically wraps the body in a uniform `ApiResponse<T>` envelope (`{ data, timestamp }`). Endpoints that need to bypass wrapping (file downloads, feeds, health checks) are meant to opt out via a `[RawResponse]` attribute. Neither the `ResponseEnvelopeMiddleware` class nor the `RawResponseAttribute` existed. Instead, every controller action manually called `ApiResponse<T>.Ok(result)` before returning, and the base-class helpers `PagedResult` / `CreatedResource` also manually wrapped their payloads. This approach is error-prone (a future controller action can forget to wrap), inconsistent with the design's separation of concerns, and does not honour the `[RawResponse]` opt-out contract described in the resolved Open Question.

**Fix applied:**
- Created `src/Blog.Api/Common/Attributes/RawResponseAttribute.cs` — attribute used to opt endpoints/controllers out of envelope wrapping.
- Created `src/Blog.Api/Middleware/ResponseEnvelopeMiddleware.cs` — buffers the response body; if the endpoint is not annotated with `[RawResponse]`, the status is 2xx, and the content type is `application/json`, re-serialises the payload inside `{ data, timestamp }`.
- Registered `ResponseEnvelopeMiddleware` in `Program.cs` immediately after `ExceptionHandlingMiddleware` and `CorrelationIdMiddleware`.
- Removed all manual `ApiResponse<T>.Ok(...)` wrapping from `ArticlesController`, `AuthController`, `DigitalAssetsController`, and from the `PagedResult` / `CreatedResource` helpers in `ApiControllerBase`.
- Annotated `AssetsController`, `SeoController`, and `DevController` with `[RawResponse]` because they return raw files, feeds, and utility responses that must not be wrapped.

**Status:** FIXED

---

## 2026-04-04 — 429 exception class named TooManyRequestsException instead of RateLimitExceededException

**Design reference:** `docs/detailed-designs/06-restful-api/README.md`, Section 7.3 — Global Exception Handler

**Description:**
The design's exception-to-status-code mapping table specifies the 429 exception class as `RateLimitExceededException`. The implementation created and used `TooManyRequestsException` in all three relevant files: `src/Blog.Api/Common/Exceptions/TooManyRequestsException.cs` (class declaration), `src/Blog.Api/Middleware/ExceptionHandlingMiddleware.cs` (catch arm), and `src/Blog.Api/Features/Auth/Commands/Login.cs` (throw site). The class name diverged from the design specification, making the codebase inconsistent with the documented contract and any tooling or future code that relies on the name given in the design.

**Fix applied:**
- Deleted `src/Blog.Api/Common/Exceptions/TooManyRequestsException.cs`.
- Created `src/Blog.Api/Common/Exceptions/RateLimitExceededException.cs` with the correct class name.
- Updated the catch arm in `ExceptionHandlingMiddleware.cs` from `TooManyRequestsException` to `RateLimitExceededException`.
- Updated the throw site in `Login.cs` from `TooManyRequestsException` to `RateLimitExceededException`.

**Status:** FIXED

---

## 2026-04-04 — RSS and Atom feed endpoint URLs do not match design specification

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.5 — FeedGenerator, Section 6.1 — L2-014

**Description:**
The design specifies RSS feed at `/feed.xml` (RSS 2.0) and Atom feed at `/atom.xml` (L2-014). The implementation routed RSS to `/feed/rss` and Atom to `/feed/atom`. This mismatch affected six locations: the `SeoController` route attributes and self-referencing `<atom:link>` URLs within both feeds, the `llms.txt` output, the `<link rel="alternate">` tags in `_Layout.cshtml`, the RSS icon link in the desktop/mobile nav, and the Feed landing page buttons. Feed readers and AI agents discovering feeds via the `<link rel="alternate">` tags or `llms.txt` would request the design-specified URLs and receive 404s.

**Fix applied:**
- Changed `[HttpGet("feed/rss")]` → `[HttpGet("feed.xml")]` and `[HttpGet("feed/atom")]` → `[HttpGet("atom.xml")]` in `SeoController`.
- Updated self-referencing `<atom:link href>` in both feeds to use the new URLs.
- Updated `llms.txt` output to reference `/feed.xml` and `/atom.xml`.
- Updated `_Layout.cshtml` alternate links, RSS nav icon, mobile menu link, and footer link.
- Updated `Feed.cshtml` subscribe buttons.

**Status:** FIXED

---

## 2026-04-04 — Content-Security-Policy header entirely absent; nonce-based CSP not implemented

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.2 — SecurityHeadersMiddleware; Open Question #1 (resolved: nonce-based CSP for v1)

**Description:**
The design requires a `Content-Security-Policy` header on every response. Open Question #1 was explicitly resolved: "Nonce-based CSP for v1. Since critical CSS is extracted automatically at build time, each inlined `<style>` block can be tagged with a per-request nonce generated by middleware. The CSP header becomes `style-src 'self' 'nonce-{random}'`, eliminating `unsafe-inline`." The implementation's inline security-headers block in `Program.cs` (lines 171-181) sets five security headers (`X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`, `Referrer-Policy`, `Permissions-Policy`) but has no `Content-Security-Policy` header at all. Without a CSP header, the platform has no browser-enforced protection against cross-site scripting, clickjacking via inline frames, or unauthorized resource loading — the primary mitigations the design lists for OWASP A01 and A05.

**Fix applied:**
- Created `src/Blog.Api/Middleware/SecurityHeadersMiddleware.cs` — generates a cryptographically-random per-request nonce (16 bytes, base-64 encoded), stores it in `HttpContext.Items["CspNonce"]` for use by Razor tag helpers, and emits the full nonce-based CSP header (`style-src 'self' 'nonce-{nonce}'`) plus all other required security headers (`HSTS`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` with `payment=()` added, `Server` header removed).
- Registered `SecurityHeadersMiddleware` in `Program.cs` immediately after `ResponseEnvelopeMiddleware`.
- Removed the previous ad-hoc inline `app.Use(...)` lambda that set an incomplete set of headers (missing CSP, missing `payment=()` in `Permissions-Policy`, incorrectly included the deprecated `X-XSS-Protection` header).

**Status:** FIXED

---

## 2026-04-04 — Sitemap article changefreq is "monthly" instead of "weekly"

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.3 — SitemapGenerator

**Description:**
The design specifies that each article `<url>` entry in the sitemap should use `<changefreq>weekly</changefreq>` for articles and `daily` for the homepage. The `SeoController.Sitemap()` method used `"monthly"` for article entries (line 86), signaling to search engines that articles change less frequently than the design intends. This could delay recrawling of updated articles.

**Status:** FIXED

---

## 2026-04-04 — SEO URLs built from request Host header instead of configured SiteUrl

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 7.3 — Canonical URL Integrity, Section 3.4 — RobotsTxtMiddleware, Section 4.6 — SiteConfiguration

**Description:**
The design explicitly states (Section 7.3): "Canonical URLs, sitemap URLs, feed URLs, and the `robots.txt` sitemap directive are constructed server-side from the known base URL in configuration, not from the incoming request's `Host` header, to prevent host header injection." The design's `SiteConfiguration` model (Section 4.6) specifies a `SiteUrl` configuration value for this purpose. The `SeoController.BaseUrl` property derived the URL from `httpContextAccessor.HttpContext.Request.Scheme` and `Request.Host`, meaning an attacker could manipulate all generated URLs in the sitemap, RSS/Atom feeds, llms.txt, and robots.txt Sitemap directive by sending a forged `Host` header. This is a known host header injection vulnerability. Additionally, `Disallow: /admin/` in robots.txt had a trailing slash inconsistent with the design's `Disallow: /admin`.

**Fix applied:**
- Added a `Site:SiteUrl` configuration key to `appsettings.json`.
- Replaced the `IHttpContextAccessor`-based `BaseUrl` property with `configuration["Site:SiteUrl"]!.TrimEnd('/')`, eliminating the host header dependency.
- Fixed `Disallow: /admin/` → `Disallow: /admin` in the robots.txt output.

**Status:** FIXED

---

## 2026-04-04 — Missing diskSpace health check on /health/ready endpoint

**Design reference:** `docs/detailed-designs/06-restful-api/README.md`, Section 6.11 — GET /health/ready

**Description:**
The design specifies that the `/health/ready` endpoint returns a response with `checks` containing both `"database"` and `"diskSpace"` health check results. The implementation in `Program.cs` only registered `.AddDbContextCheck<BlogDbContext>("database")` — no disk space health check was configured. As a result, the `/health/ready` response only reported database status, omitting the disk space check that the design requires for operational readiness monitoring. An application running on a volume nearing capacity would still report as fully healthy.

**Fix applied:**
- Created `src/Blog.Api/Common/HealthChecks/DiskSpaceHealthCheck.cs` — custom `IHealthCheck` that reads available free space on the content root drive and reports unhealthy when below 512 MB.
- Registered the check as `.AddCheck<DiskSpaceHealthCheck>("diskSpace")` in `Program.cs`.

**Status:** FIXED

---

## 2026-04-04 — RequestLoggingMiddleware absent; UseSerilogRequestLogging registered after endpoint mapping

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 3.2 — RequestLoggingMiddleware; Section 7.4 — Middleware Registration Order

**Description:**
The design specifies a dedicated `RequestLoggingMiddleware` class at `src/Blog.Api/Middleware/RequestLoggingMiddleware.cs` that: starts a `Stopwatch` before calling `next(context)`; emits a structured log entry at `Information` for 2xx/3xx responses, `Warning` for 4xx, and `Error` for 5xx; and includes the fields `Method`, `Path`, `StatusCode`, `DurationMs`, `CorrelationId`, and `Timestamp`. No such file exists in the codebase. Instead, `Program.cs` calls `app.UseSerilogRequestLogging()`, but that call is placed **after** `app.MapControllers()` and `app.MapRazorPages()` — in ASP.NET Core 8 the terminal middleware (`MapControllers`) runs before any middleware registered after it in the pipeline, so `UseSerilogRequestLogging` never intercepts requests. Even if its position were corrected, Serilog's built-in request logging uses a uniform `Information` level and does not automatically escalate to `Warning` or `Error` based on status code without explicit configuration that is absent here. The result is that all HTTP requests are logged without the designed level-based severity escalation, and the structured `DurationMs`/`CorrelationId` fields in the design's format are not guaranteed to be present.

**Fix applied:**
- Created `src/Blog.Api/Middleware/RequestLoggingMiddleware.cs` — starts a `Stopwatch` before calling `next(context)`, reads `Method`, `Path`, `StatusCode`, `DurationMs`, `CorrelationId` (from `HttpContext.Items["X-Correlation-ID"]`), and `Timestamp`, then emits the log entry at `Information` (2xx/3xx), `Warning` (4xx), or `Error` (5xx).
- Registered `app.UseMiddleware<RequestLoggingMiddleware>()` in `Program.cs` immediately after `CorrelationIdMiddleware` (ensuring the correlation ID is already in `HttpContext.Items` when the middleware fires) and before all other middleware so every request — including those resolved by `StaticFileMiddleware`, `ResponseCachingMiddleware`, and endpoint routing — is timed and logged.
- Removed the misplaced `app.UseSerilogRequestLogging()` call that was positioned after `app.MapControllers()` and was never reached during normal request processing.

**Status:** FIXED

---

## 2026-04-04 — Response compression missing Brotli/Gzip level configuration and SVG MIME type

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.2 — CompressionMiddleware

**Description:**
The design specifies Brotli at `CompressionLevel.Optimal` (level 4) for dynamic responses and Gzip at `CompressionLevel.Fastest` as a fallback. It also specifies `image/svg+xml` in the list of compressible MIME types. The implementation registered both compression providers but did not configure their compression levels (defaulting to `Fastest` for Brotli — lower compression ratio than designed) and did not add `image/svg+xml` to the MIME type list. This means Brotli responses were less compressed than designed (trading bandwidth savings for speed that was not needed given the design's intent), and SVG images were served uncompressed.

**Fix applied:**
- Added `Configure<BrotliCompressionProviderOptions>` with `CompressionLevel.Optimal`.
- Added `Configure<GzipCompressionProviderOptions>` with `CompressionLevel.Fastest`.
- Added `image/svg+xml` to the response compression MIME types via `ResponseCompressionDefaults.MimeTypes.Concat(["image/svg+xml"])`.

**Status:** FIXED

---

## 2026-04-04 — IDigitalAssetRepository missing GetByCreatedByAsync; uses unfiltered GetAllAsync instead

**Design reference:** `docs/detailed-designs/10-data-persistence/README.md`, Section 3.3 — Repository Pattern (IDigitalAssetRepository)

**Description:**
The design specifies `IDigitalAssetRepository` should expose `Task<IReadOnlyList<DigitalAsset>> GetByCreatedByAsync(Guid userId)` — a method that returns only the assets belonging to a specific creator. The implementation instead declared `Task<List<DigitalAsset>> GetAllAsync(CancellationToken cancellationToken = default)`, which fetched every digital asset in the database with no creator filter. `DigitalAssetRepository` implemented this with a plain `ToListAsync()` with no `Where` clause, and `GetDigitalAssetsHandler` called `GetAllAsync` directly. As a result, every call to list digital assets performed a full table scan and returned all assets regardless of who created them, violating the design's per-creator scoping contract and the interface contract documented in the spec.

**Fix applied:**
- Replaced `GetAllAsync` with `GetByCreatedByAsync(Guid userId, CancellationToken cancellationToken = default)` in `IDigitalAssetRepository` (return type `Task<IReadOnlyList<DigitalAsset>>`).
- Updated `DigitalAssetRepository` to implement the new method with a `Where(d => d.CreatedBy == userId)` filter and `OrderByDescending(d => d.CreatedAt)`.
- Updated `GetDigitalAssetsQuery` to accept a `UserId` parameter and `GetDigitalAssetsHandler` to pass it to `GetByCreatedByAsync`.
- Updated `DigitalAssetsController.GetAll` to extract the authenticated user's `Guid` from claims and pass it to the query.
- Updated `AdminDigitalAssetsIndexModel.OnGetAsync` (Razor Page) to pass the current userId to the query.

**Status:** FIXED

---

## 2026-04-04 — CorrelationIdMiddleware accepts any X-Correlation-Id header value without validation

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 3.1 — CorrelationIdMiddleware

**Description:**
The design specifies: "Accept it only when it matches a safe character set (`A-Z`, `a-z`, `0-9`, `-`, `_`) and length limit (64 chars). Otherwise, discard it and generate a new value." The implementation blindly accepted any value from the `X-Correlation-Id` request header — no character validation, no length check. A malicious header like `'; DROP TABLE--` or a multi-kilobyte string would be accepted, stored in `HttpContext.Items`, pushed into the Serilog `LogContext`, and echoed back in the response header. This creates a log injection vector and could pollute log aggregation systems.

**Fix applied:**
- Added a compiled `GeneratedRegex(@"^[A-Za-z0-9\-_]+$")` pattern and a 64-character length check.
- The middleware now validates the incoming header value; if it fails either check (or is empty), a new GUID is generated instead.

**Status:** FIXED

---

## 2026-04-04 — Serilog configuration: wrong formatter, missing enricher, missing log-level override

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 6.1 — Format, Section 7.2 — appsettings.json Configuration

**Description:**
The design specifies three Serilog configuration requirements that were all absent or incorrect:
1. **Console/File formatter** (Section 6.1, 7.2): Must use `CompactJsonFormatter` from `Serilog.Formatting.Compact` for compact structured JSON (`@t`, `@l`, `@mt` fields). The implementation used `Serilog.Formatting.Json.JsonFormatter` — a verbose format that does not produce the compact field names the design shows, and the `Serilog.Formatting.Compact` NuGet package was not installed.
2. **Enrichers** (Section 7.2): The design specifies `Enrich: ["FromLogContext", "WithMachineName", "WithThreadId"]`. The implementation only had `["FromLogContext", "WithMachineName"]` — missing `WithThreadId`. The `Serilog.Enrichers.Thread` and `Serilog.Enrichers.Environment` NuGet packages were also absent.
3. **MinimumLevel override** (Section 7.2): The design specifies `"Microsoft.Hosting.Lifetime": "Information"` so ASP.NET Core startup/shutdown messages are logged even though the general `Microsoft` namespace is suppressed to `Warning`. This override was missing.

**Fix applied:**
- Installed NuGet packages: `Serilog.Formatting.Compact`, `Serilog.Enrichers.Thread`, `Serilog.Enrichers.Environment`.
- Changed both Console and File sink formatters from `Serilog.Formatting.Json.JsonFormatter` to `Serilog.Formatting.Compact.CompactJsonFormatter`.
- Added `"WithThreadId"` to the `Enrich` array.
- Added `"Microsoft.Hosting.Lifetime": "Information"` to the `MinimumLevel.Override` section.
- Updated the `Using` array to include the new assemblies.

**Status:** FIXED

---

## 2026-04-04 — LogSanitizer not implemented; sensitive properties not redacted from logs

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 3.5 — LogSanitizer, Section 6.5 — Forbidden Fields

**Description:**
The design specifies a `LogSanitizer` (at `src/Blog.Api/Core/LogSanitizer.cs`) implemented as a Serilog `IDestructuringPolicy` and custom enricher that maintains a deny-list of property names (`Password`, `Token`, `Secret`, `Authorization`, `Cookie`, `CreditCard`, `SSN`, `Email`, plus variants) and replaces any matching property value with `"[REDACTED]"`, applied globally so no log sink ever receives raw sensitive data. No such component existed in the codebase — the entire PII/secret scrubbing layer specified by the design was absent. Any structured log entry containing a property named `Password`, `Token`, `Email`, etc. would be written verbatim to console and file sinks.

**Fix applied:**
- Created `src/Blog.Api/Core/LogSanitizer.cs` containing:
  - `LogSanitizer` (`IDestructuringPolicy`) with the full deny-list: `Password`, `PasswordHash`, `NewPassword`, `Token`, `AccessToken`, `RefreshToken`, `Authorization`, `Secret`, `ApiKey`, `ConnectionString`, `Cookie`, `CreditCard`, `SSN`, `Email`, `PhoneNumber`.
  - `LogSanitizingEnricher` (`ILogEventEnricher`) that iterates all properties on every log event and replaces matching values with `"[REDACTED]"`, including nested `StructureValue` properties.
- Registered `LogSanitizingEnricher` globally in `Program.cs` via `.Enrich.With<LogSanitizingEnricher>()` in the Serilog configuration lambda.

**Status:** FIXED

---

## 2026-04-04 — CORS policy allows any origin instead of configured allowlist

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.4 — CorsMiddleware

**Description:**
The design specifies a strict CORS policy: "Only origins explicitly listed in configuration are allowed. Requests from unlisted origins receive no CORS headers, causing the browser to block the response." Allowed origins are to be loaded from `appsettings.json` under `Cors:AllowedOrigins`. The implementation in `Program.cs` registered the CORS policy with `policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` — a fully-open policy that permitted every origin without restriction. The `Cors:AllowedOrigins` key was entirely absent from `appsettings.json`. This meant any web page on any domain could make cross-origin requests to the API, bypassing the cross-origin access control that the design lists as the mitigation for OWASP A01 (Broken Access Control) via CORS.

**Fix applied:**
- Added `"Cors": { "AllowedOrigins": [ "https://localhost:5001" ] }` to `appsettings.json` as the base configuration value (operators override this per environment with the real site origin).
- Replaced the open `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` CORS policy in `Program.cs` with `WithOrigins(allowedOrigins).AllowAnyHeader().WithMethods("GET","POST","PUT","PATCH","DELETE","OPTIONS").SetPreflightMaxAge(TimeSpan.FromSeconds(7200))`, reading `Cors:AllowedOrigins` from configuration at startup.

**Status:** FIXED

---

## 2026-04-04 — Required business events not logged (ArticlePublished, UserAuthenticated, UserAuthenticationFailed)

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 6.4 — Business Events

**Description:**
The design requires three business events logged at `Information` level using the structured pattern `Log.Information("Business event {EventType} occurred: {@Details}", ...)`:
1. **`ArticlePublished`** — when a post is published or updated (Section 6.4).
2. **`UserAuthenticated`** — when a user successfully logs in (Section 6.4).
3. **`UserAuthenticationFailed`** — when a login attempt fails, with no PII in details (Section 6.4).

None of these events were present anywhere in the codebase. The `PublishArticleCommandHandler` updated the article and saved without emitting any log. The `LoginCommandHandler` threw `UnauthorizedException` on failure and returned a token on success without logging either outcome. This means operators had no visibility into authentication activity or content publishing via structured business event logs.

**Fix applied:**
- `PublishArticleCommandHandler`: Injected `ILogger`, added `ArticlePublished` event after successful save when `request.Published` is true, with `ArticleId` and `Slug` in details.
- `LoginCommandHandler`: Injected `ILogger`, added `UserAuthenticationFailed` event before each `throw UnauthorizedException` with only `Reason` and optionally `UserId` (no email/password — no PII). Added `UserAuthenticated` event after successful token generation with `UserId`.

**Status:** FIXED

---

## 2026-04-04 — Article Version double-incremented: both command handlers and BlogDbContext.SaveChangesAsync increment Version

**Design reference:** `docs/detailed-designs/10-data-persistence/README.md`, Section 3.1 — BlogDbContext; `docs/detailed-designs/02-article-management/README.md`, Sections 5.2–5.4

**Description:**
Design 10 (Section 3.1) specifies that `BlogDbContext.SaveChangesAsync` "increments the article `Version` concurrency token on successful article updates" — the DbContext handles Version increment automatically for any `Modified` entity. Design 02 (Sections 5.2–5.4) also says each operation "increments Version." A prior conformance fix interpreted design 02 literally and added explicit `article.Version++` calls in `UpdateArticleCommandHandler`, `PublishArticleCommandHandler`, and `DeleteArticleCommandHandler`. However, the DbContext's `SaveChangesAsync` override (line 34-35) already performs `article.Version++` when it detects `EntityState.Modified`. The result was that every save incremented Version by 2 instead of 1 — once in the handler and once in `SaveChangesAsync`. While optimistic concurrency still functioned (the ETags were consistent within each request), the double-increment violated the design's intent of a monotonic +1 increment per mutation.

**Fix applied:**
- Removed the explicit `article.Version++` from `UpdateArticleCommandHandler`, `PublishArticleCommandHandler`, and `DeleteArticleCommandHandler`.
- The single authoritative increment now occurs in `BlogDbContext.SaveChangesAsync`, as specified by design 10 Section 3.1.

**Status:** FIXED

---

## 2026-04-04 — Kestrel MaxRequestBodySize not configured; non-file endpoints accept unlimited request bodies

**Design reference:** `docs/detailed-designs/06-restful-api/README.md`, Section 8 — Open Question 4

**Description:**
The design resolves Open Question 4: "Resolved: 1 MB. Enforced via Kestrel's `MaxRequestBodySize`. File upload endpoints override this to 10 MB." Neither the global Kestrel limit nor the per-endpoint override exists anywhere in the implementation. `Program.cs` registered no `KestrelServerOptions.Limits.MaxRequestBodySize` setting, and the `DigitalAssetsController.Upload` action carried no `[RequestSizeLimit]` or `[RequestFormLimits]` attributes. The only protection in place was an application-level `if (file.Length > MaxFileSize)` check inside `UploadDigitalAssetCommandHandler`, which runs after ASP.NET Core has already buffered the entire multipart body into memory. Without the Kestrel-level limit, non-file endpoints (e.g., `POST /api/articles`) accepted arbitrarily large request bodies, making the API vulnerable to resource exhaustion via oversized JSON payloads. The upload endpoint similarly had no server-level guard to prevent a 100 MB multipart body from being fully received before the application-level check rejected it.

**Fix applied:**
- Added `builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1 * 1024 * 1024)` in `Program.cs` immediately after `WebApplication.CreateBuilder`, setting the global default to 1 MB for all endpoints.
- Added `[RequestSizeLimit(10 * 1024 * 1024)]` and `[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]` attributes on the `DigitalAssetsController.Upload` action, overriding the global limit to 10 MB for that endpoint only.

**Status:** FIXED

---

## 2026-04-04 — MigrationRunner not implemented as IHostedService; migrations bypassed via ad-hoc startup block

**Design reference:** `docs/detailed-designs/10-data-persistence/README.md`, Section 3.5 — Migration Runner; Open Question 4 (resolved: IHostedService on startup)

**Description:**
The design resolves Open Question 4 with: "IHostedService on startup. Simplest deployment model — app self-migrates before accepting traffic." Section 3.5 specifies that `MigrationRunner` is a hosted service that calls `GetPendingMigrationsAsync()`, logs each pending migration by name, applies them via `MigrateAsync()`, logs the total duration, and terminates the application on failure. The `MigrationRunner` class at `src/Blog.Infrastructure/Data/MigrationRunner.cs` existed as a plain class with a `RunAsync()` method but was never registered or invoked. Instead, `Program.cs` contained an ad-hoc startup scope that called `db.Database.MigrateAsync()` directly — bypassing `MigrationRunner` entirely, logging only a generic success message (not individual migration names), and not using the structured per-migration timing the design requires. Because `MigrationRunner` was never registered in DI, the migration contract described in the design was not enforced.

**Fix applied:**
- Rewrote `src/Blog.Infrastructure/Data/MigrationRunner.cs` to implement `IHostedService`: uses `IServiceScopeFactory` to resolve `BlogDbContext`, calls `GetPendingMigrationsAsync()`, logs each pending migration by name, applies them via `MigrateAsync()` with per-run timing, logs the total duration, and propagates exceptions (causing application termination on migration failure).
- Added `Microsoft.Extensions.Hosting.Abstractions` package reference to `Blog.Infrastructure.csproj` to support `IHostedService`.
- Created `src/Blog.Infrastructure/Data/SeedDataHostedService.cs` — wraps the existing `SeedData` class as a second `IHostedService` so seed data runs after migrations (registration order guarantees sequencing).
- Removed the ad-hoc startup scope block from `Program.cs` that previously called `db.Database.MigrateAsync()` and `SeedData.SeedAsync()` directly.
- Registered both `MigrationRunner` and `SeedDataHostedService` in `Program.cs` via `AddHostedService<T>()`, with `MigrationRunner` registered first.

**Status:** FIXED

---

## 2026-04-04 — Missing write-endpoint rate limiting policy (60 req/min per authenticated user)

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.3 — RateLimitingMiddleware

**Description:**
The design specifies two rate limiting policies: (1) authentication endpoints at 10 req/min per IP + 5 req/15min per email, and (2) **write endpoints (POST, PUT, PATCH, DELETE) at 60 requests per minute per authenticated user**. Only the `login-ip` sliding window policy was registered in `Program.cs`. No `write-endpoints` policy existed, and no `[EnableRateLimiting]` attributes were applied to any write actions. This meant an authenticated user (or a compromised token) could issue unlimited write requests — creating, updating, publishing, and deleting articles or uploading/deleting digital assets — with no throttling, violating the abuse protection the design requires for OWASP A07 mitigation.

**Fix applied:**
- Added a `write-endpoints` sliding window rate limiter policy in `Program.cs`: 60 permits per 1-minute window, 6 segments.
- Applied `[EnableRateLimiting("write-endpoints")]` to all write actions in `ArticlesController` (Create, Update, Publish, Delete) and `DigitalAssetsController` (Upload, Delete).

**Status:** FIXED

---

## 2026-04-04 — HTTPS redirection not enabled; HTTP requests served without redirect to HTTPS

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.1 — HttpsRedirectionMiddleware, Section 5.1 — Request Security Pipeline (step 2)

**Description:**
The design specifies: "Ensures all communication occurs over TLS by redirecting HTTP requests to HTTPS. Issues a 301 Permanent Redirect from `http://` to `https://` for all requests. Configured via ASP.NET Core's built-in `UseHttpsRedirection()`." The `Program.cs` middleware pipeline did not call `app.UseHttpsRedirection()` anywhere. While the `SecurityHeadersMiddleware` correctly emits the `Strict-Transport-Security` header (which tells browsers to prefer HTTPS on subsequent visits), the initial HTTP request from a first-time visitor or a non-browser client would be served over plaintext without any redirect, defeating the HTTPS enforcement the design requires as the first defense against protocol downgrade attacks and credential interception.

**Fix applied:**
- Added `app.UseHttpsRedirection()` in the middleware pipeline after `SecurityHeadersMiddleware` and before `UseResponseCompression()`, matching the design's pipeline order (step 2 in Section 5.1).
- Gated behind `!app.Environment.IsDevelopment()` to avoid redirect loops in local development without HTTPS configured.

**Status:** FIXED

---

## 2026-04-04 — File upload validates Content-Type header instead of inspecting magic bytes; missing GIF support and dimension validation

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 3.3 — FileValidator

**Description:**
The design specifies: "Validates uploaded files by inspecting content (magic bytes), not just file extension." It lists specific byte signatures — JPEG (`FF D8 FF`), PNG (`89 50 4E 47`), WebP (`52 49 46 46...57 45 42 50`), GIF (`47 49 46 38`), AVIF (`ftypavif`) — and requires dimension validation: "rejects images whose dimensions exceed 8192×8192 or whose total pixel count exceeds 40 megapixels." The implementation validated uploads using `request.File.ContentType`, which is a client-declared header that can be trivially spoofed. A malicious file with a `Content-Type: image/jpeg` header but containing executable or HTML content would pass validation. Additionally, GIF was listed in the design's supported types but absent from the implementation's allow-list, and no dimension validation existed.

**Fix applied:**
- Replaced the `ContentType` header check with a `DetectContentTypeAsync` method that reads the first 12 bytes from the file stream and matches against the magic byte signatures for all five formats (JPEG, PNG, GIF, WebP, AVIF).
- The detected content type and correct file extension are used for storage, not the client-declared values.
- Added dimension validation: rejects images exceeding 8192×8192 or 40 megapixels before persistence.
- GIF support added via its magic bytes (`47 49 46 38`).

**Status:** FIXED

---

## 2026-04-04 — Article detail page missing semantic `<article>` and `<figure>` elements

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 7.1 — Semantic Markup and ARIA Landmarks; L2-007 — Semantic HTML

**Description:**
The design specifies (Section 7.1): "`<article>` elements wrap each ArticleCard in the listing and the full article content on the detail page." L2-007 requires: "Use `<article>`, `<header>`, `<main>`, `<nav>`, `<time datetime="...">`, `<figure>`, `<figcaption>`." The `Slug.cshtml` detail page wrapped the full article content in `<div class="article-detail">` — a non-semantic container with no meaning to assistive technologies or search engine parsers. The featured image used `<div class="article-featured-image">` instead of `<figure>`. Screen readers and structured-data crawlers cannot identify the article boundary or distinguish the featured image as a figure without the correct semantic elements.

**Fix applied:**
- Wrapped the entire article content (featured image + body) in `<article class="article-detail">`.
- Changed the featured image container from `<div class="article-featured-image">` to `<figure class="article-featured-image">` when an image is present.

**Status:** FIXED

---

## 2026-04-04 — Page titles use em-dash separator instead of pipe character

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, L2-012

**Description:**
The design specifies the meta title pattern as `{Article Title} | {Site Name}` using a pipe (`|`) separator (Section 3.1, L2-012). All six public Razor pages used an em-dash (`—`) instead: `Slug.cshtml` (`"{Title} — Quinn Brown"`), `Articles/Index.cshtml` (`"Articles — Quinn Brown"`), `Index.cshtml` (`"Quinn Brown — Personal Blog"`), `Error.cshtml`, `Feed.cshtml`, and `NotFound.cshtml`. While visually similar, the pipe separator is the SEO convention specified in the design and expected by title-parsing tools and search engine snippet generators that treat `|` as a standard site-name delimiter.

**Fix applied:**
- Replaced `—` with `|` in `ViewBag.Title` assignments across all six pages: `Slug.cshtml`, `Articles/Index.cshtml`, `Index.cshtml`, `Error.cshtml`, `Feed.cshtml`, and `NotFound.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — ICacheInvalidator not implemented; response cache never purged on article publish or update

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.1 — ResponseCachingMiddleware, Section 7.2 — Caching Strategy

**Description:**
The design specifies that when an author publishes or updates an article, the `ICacheInvalidator` service evicts the relevant entries from the in-memory response cache so the next request triggers a fresh render. Section 3.1 documents this under "Invalidation: Time-based expiry; explicit purge via `ICacheInvalidator` on content update." Section 7.2 states explicitly: "When an author publishes or updates a post, the `ICacheInvalidator` service evicts the relevant entries from the in-memory response cache." Neither an `ICacheInvalidator` interface nor any concrete implementation existed anywhere in the codebase. `PublishArticleCommandHandler` and `UpdateArticleCommandHandler` both save changes and return without calling any cache invalidation. As a result, after an article is published or its content is updated, the in-memory response cache continues serving the stale pre-change HTML for up to the 60-second TTL — meaning live readers may see outdated content immediately after a publish action, and a newly-published article's page may show draft content until the cache naturally expires.

**Fix applied:**
- Created `src/Blog.Api/Services/ICacheInvalidator.cs` — interface with a single `InvalidateArticle(string slug)` method.
- Created `src/Blog.Api/Services/CacheInvalidator.cs` — concrete singleton implementation that removes the article detail page key (`/articles/{slug}`), the listing and home page keys (`/articles`, `/`, and paged variants up to page 5) from the `IMemoryCache` that backs ASP.NET Core's `ResponseCachingMiddleware`.
- Registered `ICacheInvalidator` → `CacheInvalidator` as a singleton in `Program.cs`.
- Injected `ICacheInvalidator` into `PublishArticleCommandHandler` and called `InvalidateArticle(article.Slug)` after `SaveChangesAsync`.
- Injected `ICacheInvalidator` into `UpdateArticleCommandHandler` and called `InvalidateArticle(article.Slug)` after `SaveChangesAsync`.

**Status:** FIXED

---

## 2026-04-04 — Canonical URLs never set on public pages; `<link rel="canonical">` tag never rendered

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, L2-010 — Canonical URLs

**Description:**
The design requires (L2-010): "Canonical URLs — absolute, lowercase, no trailing slashes" and (Section 3.1): "`<link rel="canonical">` with absolute, lowercase URL, no trailing slash." The layout (`_Layout.cshtml`) correctly renders `<link rel="canonical" href="...">` when `ViewBag.CanonicalUrl` is non-empty, but no Razor page ever set this property. As a result, the canonical tag was never emitted on any page. Search engines that crawl the site via different URL variants (www vs non-www, trailing slash vs none, mixed case) would see duplicate content with no canonical signal, risking SEO penalties and diluted ranking signals.

**Fix applied:**
- `Pages/Articles/Slug.cshtml`: Injected `IConfiguration`, set `ViewBag.CanonicalUrl` to `{SiteUrl}/articles/{slug}` when the article is found.
- `Pages/Articles/Index.cshtml`: Injected `IConfiguration`, set `ViewBag.CanonicalUrl` to `{SiteUrl}/articles`.
- `Pages/Index.cshtml`: Injected `IConfiguration`, set `ViewBag.CanonicalUrl` to `{SiteUrl}` (homepage).
- All canonical URLs are built from the configured `Site:SiteUrl` (not the request Host header), absolute, lowercase, and without trailing slashes.

**Status:** FIXED

---

## 2026-04-04 — Missing og:url and og:site_name Open Graph meta tags

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, L2-009 — Open Graph metadata

**Description:**
The design specifies six Open Graph properties on every page (Section 3.1): `og:title`, `og:description`, `og:image`, `og:url`, `og:type`, `og:site_name`. The layout rendered four of these (`og:type`, `og:title`, `og:description`, `og:image`) but was missing `og:url` and `og:site_name`. Without `og:url`, social platforms that scrape the page cannot determine the canonical sharing URL, leading to duplicate share counts across URL variants. Without `og:site_name`, link previews on Facebook, LinkedIn, and messaging apps omit the site attribution that helps users identify the source.

**Fix applied:**
- Added `<meta property="og:site_name" content="Quinn Brown" />` unconditionally to `_Layout.cshtml`.
- Added `<meta property="og:url" content="@canonicalUrl" />` conditionally (when `canonicalUrl` is set), reusing the same canonical URL already computed per page.

**Status:** FIXED

---

## 2026-04-04 — RSS feed items missing author; Atom feed entries missing published date and per-entry author

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.5 — FeedGenerator, Section 4.5 — FeedEntry

**Description:**
The design specifies that each RSS `<item>` must include `<title>`, `<link>`, `<pubDate>`, `<description>`, `<author>`, and `<guid>` (Section 3.5). Each Atom `<entry>` must include `<title>`, `<link>`, `<published>`, `<updated>`, `<summary>`, `<author>`, and `<id>`. The RSS items were missing `<author>` entirely — feed readers displayed articles with no attribution. The Atom entries were missing `<published>` (the publication timestamp required by the Atom spec) and per-entry `<author>` (only the feed-level `<author>` existed, but the design's FeedEntry model specifies `AuthorName` per entry). Additionally, the Atom `<updated>` field incorrectly used `DatePublished` instead of `UpdatedAt`.

**Fix applied:**
- RSS: Added `<dc:creator>Quinn Brown</dc:creator>` to each `<item>`, using the Dublin Core namespace already imported in the feed.
- Atom: Added `<published>` element (using `DatePublished` or `CreatedAt` fallback), corrected `<updated>` to use `UpdatedAt`, and added per-entry `<author><name>Quinn Brown</name></author>`.

**Status:** FIXED

---

## 2026-04-04 — Pagination on Articles/Index page missing aria-label="Pagination"

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 3.6 — Pagination, Section 7.1 — Semantic Markup and ARIA Landmarks

**Description:**
The design specifies (Section 3.6): "Uses `<nav aria-label="Pagination">` for accessibility." The homepage (`Index.cshtml`) correctly renders `<nav class="pagination" aria-label="Pagination">`, but the Articles listing page (`Articles/Index.cshtml`) renders `<nav class="pagination">` without the `aria-label` attribute. Screen readers cannot distinguish this navigation region from the main navigation without the label, reducing accessibility for keyboard and assistive-technology users browsing the articles listing.

**Fix applied:**
- Added `aria-label="Pagination"` to the `<nav class="pagination">` element in `Articles/Index.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — IArticleRepository.GetPublishedAsync returns a tuple instead of IReadOnlyList<Article>; GetPublishedCountAsync missing

**Design reference:** `docs/detailed-designs/10-data-persistence/README.md`, Section 3.3 — Repository Pattern (IArticleRepository)

**Description:**
The design specifies two separate members on `IArticleRepository` for querying published articles:
1. `Task<IReadOnlyList<Article>> GetPublishedAsync(int page, int pageSize)` — returns only the page of items as a read-only list.
2. `Task<int> GetPublishedCountAsync()` — a dedicated method that returns the total count of published articles for pagination metadata.

The implementation collapsed both into a single method with a tuple return type: `Task<(List<Article> Items, int TotalCount)> GetPublishedAsync(int page, int pageSize)`. This diverges from the design in two ways: the return type is `List<Article>` (not `IReadOnlyList<Article>`), and `GetPublishedCountAsync()` does not exist on the interface at all. Any code that relies on the designed interface contract (e.g., test doubles, future implementations) will not find the expected method signature. `GetPublishedArticlesHandler` uses tuple destructuring to read both values, coupling it to the non-standard tuple shape instead of calling the two methods the design defines.

**Fix applied:**
- Changed `IArticleRepository.GetPublishedAsync` return type from `Task<(List<Article> Items, int TotalCount)>` to `Task<IReadOnlyList<Article>>` in `src/Blog.Domain/Interfaces/IArticleRepository.cs`.
- Added `Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)` to `IArticleRepository`.
- Updated `ArticleRepository.GetPublishedAsync` in `src/Blog.Infrastructure/Data/Repositories/ArticleRepository.cs` to return only the page of items (no count in the tuple).
- Added `ArticleRepository.GetPublishedCountAsync` that executes `CountAsync(a => a.Published)` independently.
- Updated `GetPublishedArticlesHandler` in `src/Blog.Api/Features/Articles/Queries/GetPublishedArticles.cs` to call both `GetPublishedAsync` and `GetPublishedCountAsync` separately, eliminating the tuple destructuring.

**Status:** FIXED

---

## 2026-04-04 — DigitalAsset CreatedBy FK relationship not explicitly configured with DeleteBehavior.Restrict

**Design reference:** `docs/detailed-designs/10-data-persistence/README.md`, Section 3.2 — Entity Configurations (DigitalAssetConfiguration), Section 4.5 — Relationships

**Description:**
The design specifies (Section 3.2): "CreatedBy: required FK to Users, with `DeleteBehavior.Restrict`." Table 4.5 confirms: "Asset creator | DigitalAsset | User | Many-to-one (required) | CreatedBy | Restrict." The `DigitalAssetConfiguration` only configured `builder.Property(d => d.CreatedBy).IsRequired()` — the property constraint — but did not configure the relationship itself via `HasOne`/`WithMany`/`HasForeignKey`/`OnDelete`. EF Core inferred the relationship from the `Creator` navigation property and happened to produce `ON DELETE NO ACTION` in the migration (due to SQL Server's multiple cascade path restriction), but this was accidental rather than intentional. If the cascade path constraint were ever relaxed (e.g., by changing the Article FK), EF Core's convention would flip to `Cascade`, silently deleting all of a user's uploaded assets when the user is removed.

**Fix applied:**
- Added explicit relationship configuration to `DigitalAssetConfiguration`: `builder.HasOne(d => d.Creator).WithMany().HasForeignKey(d => d.CreatedBy).OnDelete(DeleteBehavior.Restrict)`.

**Status:** FIXED

---

## 2026-04-04 — Article detail hero image missing loading="eager" and fetchpriority="high"

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.5 — ImageTagHelper; `docs/detailed-designs/03-public-article-display/README.md`, Section 3.2 — ArticleDetailPage

**Description:**
The design specifies (Section 3.5, Table): above-fold images must have `loading="eager"`, `fetchpriority="high"`, and `decoding="async"`. The article detail page's featured image is the full-width hero element at the top of the page — it is the Largest Contentful Paint (LCP) element and is always above the fold. The `<img>` tag in `Slug.cshtml` had no `loading`, `fetchpriority`, or `decoding` attributes. Without `fetchpriority="high"`, the browser deprioritizes the hero image relative to stylesheets and scripts, delaying LCP. Without `loading="eager"` (the default, but explicit is safer), lazy-loading polyfills or future browser defaults could defer it.

**Fix applied:**
- Added `loading="eager" fetchpriority="high" decoding="async"` to the featured image `<img>` tag in `Slug.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — Article listing card images missing decoding="async"

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.5 — ImageTagHelper

**Description:**
The design specifies (Section 3.5, Table): below-fold images must have `loading="lazy"` **and** `decoding="async"`. The article card images on both the homepage (`Index.cshtml`) and the articles listing page (`Articles/Index.cshtml`) had `loading="lazy"` but were missing `decoding="async"`. Without `decoding="async"`, the browser blocks the main thread while decoding each image, which can increase INP (Interaction to Next Paint) on pages with multiple card images, especially on lower-powered mobile devices.

**Fix applied:**
- Added `decoding="async"` to the article card `<img>` tags in both `Index.cshtml` and `Articles/Index.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — Public pages missing Cache-Control headers; ResponseCachingMiddleware never activates for HTML pages

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.1 — ResponseCachingMiddleware, Section 7.2 — Caching Strategy

**Description:**
The design specifies that all public HTML pages (article detail, articles listing, homepage) must be served with `Cache-Control: max-age=60, stale-while-revalidate=600` (Section 3.1: "Cache profiles are applied per-route — Article pages: `max-age=60, stale-while-revalidate=600`; Home / listing pages: `max-age=60, stale-while-revalidate=600`"). `Program.cs` correctly registers `app.UseResponseCaching()`, but ASP.NET Core's `ResponseCachingMiddleware` will only cache and serve a response from cache if the response carries a `Cache-Control: public, max-age=N` header. Without a `[ResponseCache]` attribute or equivalent header on the page model, no cache headers are set and the middleware is inert for every public page request. The three public Razor Page models — `IndexModel` (homepage), `ArticlesIndexModel` (listing), and `ArticleDetailModel` (detail) — had no `[ResponseCache]` attribute and set no `Cache-Control` headers. Every request therefore triggered a full database query and Razor re-render, negating the performance benefit the design requires.

**Fix applied:**
- Registered a named `"HtmlPage"` cache profile in `Program.cs` via `.AddRazorPages().AddMvcOptions(...)` with `Duration = 60`, `Location = Any`, and `VaryByHeader = "Accept-Encoding"`.
- Added `[ResponseCache(CacheProfileName = "HtmlPage")]` to `IndexModel`, `ArticlesIndexModel`, and `ArticleDetailModel`.
- Added `Response.Headers.Append("Cache-Control", "public, max-age=60, stale-while-revalidate=600")` in each page's `OnGetAsync` (after successful data retrieval for the detail page) so browsers and intermediate caches receive the full designed directive including the `stale-while-revalidate=600` extension that `[ResponseCache]` alone does not emit.

**Status:** FIXED

---

## 2026-04-04 — og:type hardcoded to "website" on all pages including article detail

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, Section 6.1 — L2-009

**Description:**
The design specifies (Section 6.1, L2-009): "`og:type` is 'article' for article pages and 'website' for other pages." The layout hardcoded `<meta property="og:type" content="website" />` on every page including article detail pages. Social platforms (Facebook, LinkedIn) use `og:type` to determine how to render link previews — an `article` type triggers richer previews with author/publication metadata, while `website` produces a generic card. Serving `og:type=website` for article pages means social shares lose the richer article preview format.

**Fix applied:**
- Added `var ogType = ViewBag.OgType ?? "website"` to `_Layout.cshtml` and changed the hardcoded tag to `<meta property="og:type" content="@ogType" />`.
- Set `ViewBag.OgType = "article"` in `Slug.cshtml` when the article is found. All other pages default to `"website"`.

**Status:** FIXED

---

## 2026-04-04 — SlugRedirectMiddleware not implemented; uppercase and trailing-slash article URLs served without 301 redirect

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.7 — SlugRedirectMiddleware, L2-015 — Clean URL Structure

**Description:**
The design specifies (Section 3.7): "Intercepts incoming requests to `/articles/{slug}` paths. If the slug contains uppercase characters, issues a 301 redirect to the lowercase version. If the URL has a trailing slash, issues a 301 redirect to the version without it." The design also specifies (Section 6.2) that this middleware runs before routing. No `SlugRedirectMiddleware` existed anywhere in the codebase. As a result, requests to `/articles/My-Article` or `/articles/my-article/` would either 404 or serve content at a non-canonical URL, creating duplicate content visible to search engines and fragmenting link equity across URL variants.

**Fix applied:**
- Created `src/Blog.Api/Middleware/SlugRedirectMiddleware.cs` that intercepts `/articles/*` requests, checks for uppercase characters and trailing slashes, and returns 301 with the corrected lowercase, no-trailing-slash URL.
- Registered `app.UseMiddleware<SlugRedirectMiddleware>()` in `Program.cs` before `UseStaticFiles()` and `UseRouting()`, matching the design's pipeline order (Section 6.2).

**Status:** FIXED

---

## 2026-04-04 — JSON-LD structured data not implemented on any page

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.2 — JsonLdGenerator, L2-008 — Structured Data

**Description:**
The design specifies (Section 3.2, L2-008): "Article pages emit a `Schema.org/Article` object with: `headline`, `datePublished`, `dateModified`, `author` (Person), `description`, `image`, `publisher` (Organization with logo), `mainEntityOfPage`." and "Listing pages emit a `Schema.org/Blog` object." No `<script type="application/ld+json">` tag existed on any page in the codebase. Without JSON-LD, search engines cannot extract structured article metadata for rich snippets (headline, date, author attribution in search results), and the site loses eligibility for Google's Article rich results — a significant SEO gap given the design's goal of "perfect SEO rating across all automated audit tools."

**Fix applied:**
- `Slug.cshtml`: Added a `@section Head` block with `<script type="application/ld+json">` containing a `Schema.org/Article` object with `headline`, `datePublished`, `dateModified`, `description`, `mainEntityOfPage`, `author` (Person), and `publisher` (Organization). String values are JavaScript-encoded to prevent XSS.
- `Index.cshtml`: Added a `@section Head` block with a `Schema.org/Blog` object containing `name`, `description`, and `url`.

**Status:** FIXED

---

## 2026-04-04 — twitter:card hardcoded to "summary_large_image"; twitter:image tag missing

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, Section 6.1 — L2-009

**Description:**
The design specifies (Section 6.1, L2-009): "`twitter:card` is 'summary_large_image' when an image is present, 'summary' otherwise." The layout hardcoded `<meta name="twitter:card" content="summary_large_image" />` on every page regardless of whether an image was available. Pages without a featured image (most listing pages, homepage) incorrectly declared `summary_large_image`, causing Twitter/X to attempt to render a large image card with no image — resulting in a broken or blank preview. Additionally, the `twitter:image` meta tag was entirely absent, so even pages with images never communicated the image URL to Twitter's card crawler.

**Fix applied:**
- Made `twitter:card` conditional: renders `"summary_large_image"` when `ogImage` is set, `"summary"` otherwise.
- Added `<meta name="twitter:image" content="@ogImage" />` conditionally when an image is present.

**Status:** FIXED

---

## 2026-04-04 — PaginationHelper not implemented; PreviousPageUrl and NextPageUrl always null in paginated responses

**Design reference:** `docs/detailed-designs/06-restful-api/README.md`, Section 3.3 — PaginationHelper

**Description:**
The design specifies a `PaginationHelper` component (Section 3.3) that "generates `NextPageUrl` and `PreviousPageUrl` using ASP.NET Core link generation from route values and configured application URLs rather than blindly echoing raw host headers." The `PagedResponse<T>` model declares both `PreviousPageUrl` and `NextPageUrl` string properties, but neither is ever populated. Both `GetArticlesHandler` and `GetPublishedArticlesHandler` construct `PagedResponse<T>` objects leaving these fields as `null`. The `ApiControllerBase.PagedResult` helper simply calls `Ok(response)` without injecting navigation URLs. As a result, every paginated API response (`GET /api/articles` and `GET /api/public/articles`) returns `"previousPageUrl": null` and `"nextPageUrl": null` regardless of the current page, making it impossible for API consumers and headless clients to navigate pages programmatically without constructing URLs themselves.

**Fix applied:**
- Created `src/Blog.Api/Common/Models/PaginationHelper.cs` — static class with `SetNavigationUrls<T>(PagedResponse<T>, string baseUrl, string requestPath)` that builds `PreviousPageUrl` and `NextPageUrl` using the configured `Site:SiteUrl` base (not the raw `Host` header), appending `?page=N&pageSize=N` query parameters.
- Updated `ApiControllerBase` to accept `IConfiguration` in its primary constructor and call `PaginationHelper.SetNavigationUrls` inside the `PagedResult` helper before returning `Ok(response)`.
- Updated `ArticlesController`, `AuthController`, and `DigitalAssetsController` primary constructors to forward `IConfiguration` to `ApiControllerBase`.

**Status:** FIXED

---

## 2026-04-04 — Footer missing "Articles" navigation link

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 3.4 — Footer

**Description:**
The design specifies (Section 3.4): "Displays navigation links (Articles, About, RSS), copyright notice, and optional social links." The footer in `_Layout.cshtml` contained links to RSS Feed, Atom Feed, and Sitemap, plus the copyright notice, but was missing the "Articles" navigation link. The "About" page does not exist in the v1 scope so its absence is expected, but "Articles" is a core page that the design explicitly lists as a footer navigation item.

**Fix applied:**
- Added `<a href="/articles">Articles</a>` as the first link in the footer's `footer-links` div in `_Layout.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — Missing dns-prefetch fallback hints for font origins

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.6 — ResourceHintTagHelper

**Description:**
The design specifies (Section 3.6): "`dns-prefetch`: Fallback for browsers without `preconnect` support." The layout included `<link rel="preconnect">` tags for `fonts.googleapis.com` and `fonts.gstatic.com` but had no corresponding `<link rel="dns-prefetch">` fallback tags. Older browsers (e.g., Safari < 11.1, IE 11) do not support `preconnect` but do support `dns-prefetch`. Without the fallback, those browsers perform DNS resolution lazily when the font stylesheet is first fetched, adding 50-100ms to the critical rendering path on the first visit.

**Fix applied:**
- Added `<link rel="dns-prefetch" href="https://fonts.googleapis.com" />` and `<link rel="dns-prefetch" href="https://fonts.gstatic.com" />` alongside the existing `preconnect` hints in `_Layout.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — HtmlSanitizer uses default allow-list instead of the design-specified minimal allow-list

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.7 — HtmlSanitizer

**Description:**
The design specifies that article body HTML must be sanitized using the `HtmlSanitizer` NuGet package (Ganss.Xss) "with a **configured allow-list** of tags and attributes" — preserving only: `<p>`, `<h1>`-`<h6>`, `<a>`, `<img>`, `<ul>`, `<ol>`, `<li>`, `<strong>`, `<em>`, `<code>`, `<pre>`, `<blockquote>`, `<figure>`, `<figcaption>`. The `MarkdownConverter` constructor calls `new HtmlSanitizer()` with no configuration, which defaults to Ganss.Xss's built-in permissive allow-list. That default list includes many elements outside the design's scope (e.g., `<table>`, `<div>`, `<span>`, `<input>`, `<form>`, `<select>`, `<button>`, `<details>`, `<summary>`, and others), none of which appear in the design's specified allow-list. While the default list does strip `<script>`, `<iframe>`, and inline event handlers, it allows a significantly broader set of elements than the design intends, increasing the HTML surface area that could be leveraged for future XSS or injection vectors (e.g., a `<form>` that mimics a login prompt, or `<input>` elements embedded in content). The design chose a minimal allow-list deliberately to reduce the attack surface to only the tags required for Markdown-derived article content.

**Fix applied:**
- Added a `BuildSanitizer()` static factory method in `src/Blog.Api/Services/MarkdownConverter.cs` that constructs a `HtmlSanitizer` with its `AllowedTags`, `AllowedAttributes`, and `AllowedSchemes` collections explicitly cleared and then populated with only the design-specified values.
- **Allowed tags** (14 total): `p`, `h1`–`h6`, `a`, `img`, `ul`, `ol`, `li`, `strong`, `em`, `code`, `pre`, `blockquote`, `figure`, `figcaption`.
- **Allowed attributes** (7 total): `href`, `src`, `alt`, `title`, `width`, `height`, `id` (for heading anchors generated by Markdig).
- **Allowed URI schemes** (3 total): `https`, `http`, `mailto` — blocking `javascript:` URLs on `href`/`src`.
- Changed the instance field from `new HtmlSanitizer()` to the pre-built static singleton `BuildSanitizer()` to avoid re-allocating the configuration on every `MarkdownConverter` instantiation.

**Status:** FIXED

---

## 2026-04-04 — DigitalAssetDto exposes internal StoredFileName field

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 4.2 — DTOs (DigitalAssetDto)

**Description:**
The design's `DigitalAssetDto` (Section 4.2) specifies these fields: `DigitalAssetId`, `OriginalFileName`, `Url`, `ContentType`, `FileSizeBytes`, `Width`, `Height`, `CreatedAt`. The implementation's `DigitalAssetDto` record included an extra `StoredFileName` field — an internal storage implementation detail (the GUID-based filename on disk). Exposing `StoredFileName` in the API response leaks the file storage naming convention to clients, which is unnecessary since the `Url` field already provides the public serving path. The admin page also referenced `StoredFileName` directly instead of using the `Url` property.

**Fix applied:**
- Removed `StoredFileName` from the `DigitalAssetDto` record definition.
- Updated all three construction sites (`GetDigitalAssetsHandler`, `GetDigitalAssetByIdHandler`, `UploadDigitalAssetCommandHandler`) to omit `StoredFileName` while keeping the `Url` field populated from it internally.
- Updated the admin assets page (`Admin/DigitalAssets/Index.cshtml`) to use `@asset.Url` instead of `/assets/@asset.StoredFileName`.

**Status:** FIXED

---

## 2026-04-04 — Invalid file type and dimension errors return 409 Conflict instead of 400 Bad Request

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 6.1 — POST /api/digital-assets

**Description:**
The design specifies (Section 6.1): invalid file type returns "Error Response (400 Bad Request)" with detail "File type not allowed." The `UploadDigitalAssetCommandHandler` threw `ConflictException` for three validation scenarios — invalid file type, oversized dimensions, and excessive pixel count — all of which mapped to 409 Conflict via the exception middleware. The 409 status code is reserved for state conflicts (e.g., duplicate slug), not input validation failures. Clients receiving 409 for a bad upload would interpret it as a server-side state conflict and might retry, whereas 400 correctly signals that the request itself is invalid and should not be retried without modification.

**Fix applied:**
- Created `src/Blog.Api/Common/Exceptions/BadRequestException.cs`.
- Added a `BadRequestException` → 400 mapping in `ExceptionHandlingMiddleware`.
- Changed the three validation `throw` sites in `UploadDigitalAssetCommandHandler` from `ConflictException` to `BadRequestException`.

**Status:** FIXED

---

## 2026-04-04 — ASP.NET Core rate limiter middleware returns 429 without Retry-After header

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.3 — RateLimitingMiddleware

**Description:**
The design specifies (Section 3.3): "When the limit is exceeded, returns HTTP 429 Too Many Requests with a `Retry-After` header indicating the number of seconds until the window resets." The `AddRateLimiter` configuration in `Program.cs` set `RejectionStatusCode = 429` but did not configure an `OnRejected` callback. ASP.NET Core's built-in rate limiter middleware does not automatically emit a `Retry-After` header — it returns a bare 429 with an empty body. Without `Retry-After`, well-behaved clients have no guidance on when to retry, leading to either immediate retries (worsening the load) or arbitrary backoff (degrading user experience). The response body was also empty instead of the RFC 7807 Problem Details format used by all other error responses.

**Fix applied:**
- Added an `OnRejected` callback to the rate limiter options that:
  1. Reads `RetryAfter` metadata from the `RateLimitLease` if available, falling back to 60 seconds.
  2. Sets the `Retry-After` response header.
  3. Writes an RFC 7807 JSON problem details body matching the format used by `ExceptionHandlingMiddleware`.

**Status:** FIXED

---

## 2026-04-04 — PaginationParameters.Page accepts zero or negative values

**Design reference:** `docs/detailed-designs/06-restful-api/README.md`, Section 4.4 — PaginationParameters

**Description:**
The design specifies (Section 4.4): "Page: Minimum 1, default 1." The `PaginationParameters` class clamped `PageSize` to [1, 100] via a custom setter but left `Page` as an auto-property with no validation. A query string like `?page=0` or `?page=-5` would be accepted, producing a negative `Skip` value (`(Page - 1) * PageSize`), which translates to a negative SQL `OFFSET` — causing a database query error or returning unexpected results depending on the provider.

**Fix applied:**
- Changed `Page` from an auto-property to a backing-field property with a setter that clamps values below 1 to 1, matching the pattern already used for `PageSize`.

**Status:** FIXED

---

## 2026-04-04 — 429 responses missing Retry-After header

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 7.3 — Rate Limiting on Login; `docs/detailed-designs/08-security-hardening/README.md`, Section 3.3 — RateLimitingMiddleware

**Description:**
Both design documents explicitly require that when a rate limit is exceeded the response includes a `Retry-After` header indicating the number of seconds until the sliding window resets. The authentication design (Section 7.3) states: "the endpoint returns `429 Too Many Requests` with a `Retry-After` header indicating the number of seconds until the window resets." The security hardening design (Section 3.3) states the same. The `ExceptionHandlingMiddleware` catches `RateLimitExceededException` and writes a 429 ProblemDetails body, but never sets the `Retry-After` header on the response. `IEmailRateLimitService.TryAcquire` returns only a `bool` and has no mechanism to surface the reset time, so even if the middleware wanted to emit the header it had no data to populate it with. Clients (browsers, API consumers) that respect `Retry-After` to implement automatic back-off therefore had no delay hint and would immediately retry — the opposite of what rate limiting is designed to achieve.

**Fix applied:**
- Updated `IEmailRateLimitService.TryAcquire` signature to `bool TryAcquire(string email, out int retryAfterSeconds)`, returning the number of whole seconds until the oldest attempt slides out of the 15-minute window.
- Updated `EmailRateLimitService.TryAcquire` to compute `retryAfterSeconds` as `ceil((oldestAttempt + Window - now).TotalSeconds)` when the quota is exhausted, with a minimum of 1 second.
- Updated `RateLimitExceededException` to carry a `RetryAfterSeconds` property (constructor parameter with default 0).
- Updated `LoginCommandHandler` to pass `retryAfterSeconds` from `TryAcquire` into the `RateLimitExceededException`.
- Updated `ExceptionHandlingMiddleware` to set `Response.Headers["Retry-After"]` when the caught exception is a `RateLimitExceededException` with `RetryAfterSeconds > 0`.

**Status:** FIXED

---

## 2026-04-04 — Sitemap includes non-article pages (/articles, /feed) contrary to design resolution

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.3 — SitemapGenerator, Section 8 — Open Question #5

**Description:**
The design resolves Open Question #5: "Published articles only. Non-article pages (about, contact) are few and static; search engines discover them via internal links. Keeping the sitemap article-only simplifies generation." Section 3.3 confirms: "each `<url>` entry for each article plus the homepage." The `SeoController.Sitemap()` method included three static entries — the homepage (`/`), `/articles`, and `/feed` — before appending individual article URLs. The `/articles` and `/feed` entries violate the design's explicit resolution to limit the sitemap to the homepage and individual published articles only.

**Fix applied:**
- Removed the `/articles` and `/feed` static entries from the sitemap URL list in `SeoController.Sitemap()`. Only the homepage (`/`) and individual published article URLs remain.

**Status:** FIXED

---

## 2026-04-04 — ETagGenerator not implemented; article detail page never returns 304 Not Modified

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.7 — ETagGenerator; Section 5.1 — Full Page Delivery Pipeline (step 8)

**Description:**
The design specifies an `IETagGenerator` service that "computes a weak validator from the page version metadata" and, when the incoming `If-None-Match` request header matches, short-circuits the response with `304 Not Modified` (Section 5.1, step 8: "If `If-None-Match` matches, short-circuits with 304 Not Modified"). This is confirmed by the RESTful API design Open Question 6 resolution: "weak validators on cacheable GET responses." Neither the `IETagGenerator` interface nor any implementation existed in the codebase. The `ArticleDetailModel` (`Pages/Articles/Slug.cshtml.cs`) always re-queries the database and re-renders the page regardless of whether the client already holds a current version. Without the 304 path, every repeat visit from a browser or CDN that already has the page in its local cache still incurs a full database round-trip and Razor render, even when the article content has not changed since the last visit. The `AssetsController` already implements the same pattern correctly for binary files via `If-None-Match` → 304.

**Fix applied:**
- Created `src/Blog.Api/Services/IETagGenerator.cs` — interface with `Generate(Guid articleId, int version)` and `IsMatch(string etag, string? ifNoneMatch)` methods.
- Created `src/Blog.Api/Services/ETagGenerator.cs` — singleton implementation that produces weak ETags (`W/"article-{id}-v{version}"`) and parses comma-separated `If-None-Match` header values, including the `*` wildcard.
- Registered `IETagGenerator` → `ETagGenerator` as a singleton in `Program.cs`.
- Updated `ArticleDetailModel` (`Pages/Articles/Slug.cshtml.cs`) to inject `IETagGenerator`, compute the ETag after a successful article fetch, compare it against the `If-None-Match` request header, and return `StatusCode(304)` when the values match. When no match, the `ETag` response header is set alongside the existing `Cache-Control` header so subsequent requests can trigger 304s.

**Status:** FIXED

---

## 2026-04-05 — Featured image URLs render bare GUID without file extension; images 404 on public pages

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 4.2 — PublicArticleDto (`FeaturedImageUrl: string?`)

**Description:**
The design specifies `FeaturedImageUrl` (string?) as a resolved URL in the article DTOs. The implementation passed `FeaturedImageId` (Guid?) — the raw FK value — to listing DTOs and Razor pages. Pages rendered `<img src="/assets/{GUID}">` (e.g., `/assets/a1b2c3d4-...`) but digital assets are stored with file extensions (e.g., `a1b2c3d4-....jpg`). The `AssetsController` resolves files by exact filename match, so the extensionless URL would 404, producing broken images on every article card and detail page with a featured image.

**Fix applied:**
- Added `FeaturedImageUrl` (string?) to both `ArticleListDto` and `ArticleDto`.
- Added `.Include(a => a.FeaturedImage)` to `GetAllAsync` and `GetPublishedAsync` repository methods so the navigation property is loaded for listing queries.
- All six DTO construction sites (`GetArticlesHandler`, `GetPublishedArticlesHandler`, `GetArticleByIdHandler`, `GetArticleBySlugHandler`, `UpdateArticleCommandHandler`, `PublishArticleCommandHandler`, `CreateArticleCommandHandler`) now resolve `FeaturedImageUrl` from `article.FeaturedImage?.StoredFileName`.
- Updated `Index.cshtml`, `Articles/Index.cshtml`, and `Articles/Slug.cshtml` to use `FeaturedImageUrl` instead of the bare `FeaturedImageId`.

**Status:** FIXED

---

## 2026-04-04 — PublicArticleController absent; GET /api/public/articles endpoints not implemented

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 3.7 — PublicArticleController

**Description:**
The design specifies a `PublicArticleController` (Section 3.7) that exposes two unauthenticated endpoints:
1. `GET /api/public/articles?page={page}&pageSize={pageSize}` — returns a paginated list of published articles.
2. `GET /api/public/articles/{slug}` — returns a single published article by slug, returning 404 if the article is not found or not published.

Neither endpoint exists anywhere in the codebase. The only `ArticlesController` is guarded by `[Authorize]` and routes to `/api/articles` — meaning external consumers (feed readers, headless clients, or integrations) that follow the documented public API contract receive 401 instead of article data. Additionally, the existing `GetArticleBySlugQuery` does not filter for `Published == true`, so even if wired to a public endpoint it would incorrectly return draft articles to unauthenticated callers. The `GetPublishedArticlesQuery` and handler already exist but have no HTTP surface.

**Fix applied:**
- Created `src/Blog.Api/Features/Articles/Queries/GetPublishedArticleBySlug.cs` — `GetPublishedArticleBySlugQuery` and its handler, which calls `GetBySlugAsync` and then checks `article.Published`; returns 404 (via `NotFoundException`) if the article does not exist or is in draft status.
- Created `src/Blog.Api/Controllers/PublicArticlesController.cs` — `[ApiController]` at `[Route("api/public/articles")]`, no `[Authorize]`. `GET /` dispatches to `GetPublishedArticlesQuery` and returns a `PagedResult`. `GET /{slug}` dispatches to `GetPublishedArticleBySlugQuery` and returns `Ok`.

**Status:** FIXED

---

## 2026-04-05 — Admin article editor featured image also renders bare FeaturedImageId GUID

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 4.2 — PublicArticleDto; `docs/detailed-designs/02-article-management/README.md`, Section 7.4 — Article Editor

**Description:**
The previous conformance fix (featured image URLs on public pages) missed the admin article editor page. `Admin/Articles/Edit.cshtml` rendered `<img src="/assets/@Model.Article.FeaturedImageId">` using the bare GUID FK value without the file extension. Since `AssetsController` resolves files by exact filename (including extension), the featured image preview in the editor would 404, showing a broken image to the admin user editing an article. The `ArticleDto` already has the `FeaturedImageUrl` field populated from the previous fix.

**Fix applied:**
- Updated `Admin/Articles/Edit.cshtml` to check `!string.IsNullOrEmpty(Model.Article?.FeaturedImageUrl)` and use `@Model.Article.FeaturedImageUrl` as the image `src`, matching the pattern used on the public pages.

**Status:** FIXED

---

## 2026-04-05 — Sitemap lastmod uses DatePublished instead of UpdatedAt

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.3 — SitemapGenerator

**Description:**
The design specifies (Section 3.3): "Each `<url>` entry includes `<lastmod>` (article's last modified date in W3C format)." The implementation used `(article.DatePublished ?? article.CreatedAt)` for the `<lastmod>` value. The "last modified date" is `UpdatedAt`, not `DatePublished` — an article edited after publication would show its original publish date instead of the actual last-modification date. This gives search engine crawlers stale information about content freshness, potentially delaying recrawls of updated articles since the crawler sees an unchanged `<lastmod>` despite content changes.

**Fix applied:**
- Changed the sitemap `<lastmod>` value from `(article.DatePublished ?? article.CreatedAt).ToString("yyyy-MM-dd")` to `article.UpdatedAt.ToString("yyyy-MM-dd")` in `SeoController.Sitemap()`.

**Status:** FIXED

---

## 2026-04-05 — Meta title and description not truncated to SEO length limits

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, L2-012

**Description:**
The design specifies (Section 3.1, L2-012): "Title pattern `{Article Title} | {Site Name}`, truncated to 60 characters" and "`<meta name="description">` with article abstract or page description, truncated to 160 characters at the nearest word boundary." The layout used `ViewBag.Title` and `ViewBag.Description` values without any truncation. Article titles can be up to 256 characters (DB constraint) plus the ` | Quinn Brown` suffix (15 chars), potentially producing a 271-character `<title>` tag. Abstracts can be up to 512 characters. Search engines truncate titles beyond ~60 chars and descriptions beyond ~160 chars with ellipsis in search results, and excessively long values can cause SEO audit tools (Lighthouse, Screaming Frog) to flag the pages as non-compliant.

**Fix applied:**
- Added truncation in `_Layout.cshtml`: title truncated to 60 characters (with `...` ellipsis if exceeded), description truncated to 160 characters at the nearest word boundary using a `TruncateAtWord` helper function.

**Status:** FIXED

---

## 2026-04-04 — Serilog.Enrichers.Thread NuGet package missing; WithThreadId enricher referenced but unresolvable

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 7.2 — appsettings.json Configuration, Section 7.3 — NuGet Packages

**Description:**
The design lists `Serilog.Enrichers.Thread` as a required NuGet package (Section 7.3) and specifies `"WithThreadId"` in the `Enrich` array of `appsettings.json` (Section 7.2). A prior conformance fix added `"Serilog.Enrichers.Thread"` to the `Using` array and `"WithThreadId"` to the `Enrich` array in `appsettings.json`, but never added the `<PackageReference Include="Serilog.Enrichers.Thread">` entry to `Blog.Api.csproj`. As a result, `Serilog.Settings.Configuration` — which uses reflection to resolve enricher types from the assembly names in `Using` — cannot load `Serilog.Enrichers.Thread` at startup. The assembly is absent from the build output, causing the `WithThreadId` enricher to be silently skipped (or throw a `TypeLoadException` depending on Serilog version), meaning thread IDs are never included in structured log entries despite the configuration specifying them.

**Fix applied:**
- Added `<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />` to `src/Blog.Api/Blog.Api.csproj`, immediately after the `Serilog.Enrichers.Environment` entry.
- Ran `dotnet restore` to confirm the package resolves correctly. The `WithThreadId` enricher now loads from the installed assembly at startup, and thread IDs are included in all structured log entries.

**Status:** FIXED

---

## 2026-04-04 — Serilog.Sinks.ApplicationInsights missing; ApplicationInsights sink not configured

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 7.1 — Serilog Sinks, Section 7.2 — appsettings.json Configuration, Section 7.3 — NuGet Packages

**Description:**
The design specifies (Section 7.1): "ApplicationInsights: Staging, Production — Centralized log aggregation, KQL querying, dashboards, and alerting via `Serilog.Sinks.ApplicationInsights`." Section 7.3 lists two required NuGet packages for this: `Serilog.Sinks.ApplicationInsights` and `Microsoft.ApplicationInsights.AspNetCore`. Section 7.2 shows the `appsettings.json` `WriteTo` array including an `ApplicationInsights` entry with `telemetryConverter: "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"`. Neither package appears in `Blog.Api.csproj`, `Serilog.Sinks.ApplicationInsights` is absent from the `Using` array in `appsettings.json`, and no `ApplicationInsights` sink entry exists in the `WriteTo` array. As a result, production and staging deployments emit logs only to Console and to the rolling file — structured log entries never reach Azure Monitor, making KQL querying, dashboards, alerting, and the 30-day log retention described in Open Question 4 completely unavailable.

**Fix applied:**
- Added `<PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />` and `<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />` to `src/Blog.Api/Blog.Api.csproj`.
- Added `"Serilog.Sinks.ApplicationInsights"` to the `Using` array in `appsettings.json` so `Serilog.Settings.Configuration` can resolve the sink type by reflection at startup.
- Added the `ApplicationInsights` sink entry to the `WriteTo` array in `appsettings.json` with `telemetryConverter: "Serilog.Sinks.ApplicationInsights.TelemetryConverters.TraceTelemetryConverter, Serilog.Sinks.ApplicationInsights"`, matching the design's Section 7.2 configuration exactly.

**Status:** FIXED

---

## 2026-04-05 — JWT signing key minimum size (256 bits) not validated at startup

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 7.4 — Additional Measures

**Description:**
The design specifies (Section 7.4): "The JWT signing key must be at least 256 bits." The `Program.cs` JWT configuration read `Jwt:Secret` from configuration and passed it directly to `SymmetricSecurityKey` without any size validation. HMAC-SHA256 accepts shorter keys by padding them internally, so a misconfigured short secret (e.g., `"mysecret"` — 7 bytes) would not cause a startup error but would produce cryptographically weak tokens vulnerable to brute-force attacks. The `appsettings.json` placeholder value is long enough, but there was no guard against an operator deploying with a truncated or placeholder-length secret below the security threshold.

**Fix applied:**
- Added a startup check in `Program.cs` immediately after reading `Jwt:Secret`: if `Encoding.UTF8.GetByteCount(jwtSecret) < 32`, the application throws `InvalidOperationException` with a clear message and refuses to start.
- Reused the validated `jwtSecret` variable for the `IssuerSigningKey` construction to avoid reading the config value twice.

**Status:** FIXED

---

## 2026-04-05 — Database health check has no timeout; hung connection blocks /health/ready indefinitely

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 3.4 — DbHealthCheck

**Description:**
The design specifies (Section 3.4): "Applies a timeout of 5 seconds to prevent hanging." The `AddDbContextCheck<BlogDbContext>("database")` registration in `Program.cs` used the default timeout of `Timeout.InfiniteTimeSpan`. If the database server becomes unreachable or a connection hangs (e.g., firewall silently dropping packets), the health check would block indefinitely. Load balancers and Kubernetes probes polling `/health/ready` would time out on their side, but the server thread remains blocked, eventually exhausting the thread pool under sustained connectivity issues.

**Fix applied:**
- Added a post-configuration block using `Configure<HealthCheckServiceOptions>` that sets `Timeout = TimeSpan.FromSeconds(5)` on the `"database"` health check registration, matching the design's 5-second requirement.

**Status:** FIXED

---

## 2026-04-05 — Health check endpoints not excluded from ResponseEnvelopeMiddleware

**Design reference:** `docs/detailed-designs/06-restful-api/README.md`, Section 3.4 — ResponseEnvelopeMiddleware

**Description:**
The design specifies (Section 3.4): "Skips wrapping for streaming responses (e.g., file downloads) and health check endpoints." The `ResponseEnvelopeMiddleware` only checked for the `[RawResponse]` attribute to skip wrapping. The `/health/ready` endpoint uses a custom response writer that sets `Content-Type: application/json`, so the middleware's `isJson` check passed and the health check response was wrapped in the `{ data, timestamp }` envelope. This produced `{ "data": { "status": "healthy", "checks": { ... } }, "timestamp": "..." }` instead of the design's plain `{ "status": "healthy", "checks": { ... } }`. Load balancers and monitoring tools parsing the health check response would fail to find the expected top-level `status` field.

**Fix applied:**
- Added `|| context.Request.Path.StartsWithSegments("/health")` to the `skipEnvelope` condition in `ResponseEnvelopeMiddleware`, so both `/health` and `/health/ready` endpoints bypass envelope wrapping.

**Status:** FIXED

---

## 2026-04-04 — SlugRedirectMiddleware does not return 404 for file-extension or numeric-ID slug patterns

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.7 — SlugRedirectMiddleware

**Description:**
The design specifies (Section 3.7): "If the URL contains file extensions or numeric IDs, returns 404 (these patterns are not valid)." The implementation in `SlugRedirectMiddleware` handles only two of the three documented behaviours — it issues 301 redirects for uppercase slugs and trailing slashes — but it never checks for file extensions (e.g., `/articles/my-post.html`, `/articles/my-post.php`) or pure numeric IDs (e.g., `/articles/12345`). Both patterns are invalid under the clean-URL contract the design establishes: the platform uses hyphenated slug strings only, never extensions or numeric IDs. Without the middleware check, a request to `/articles/my-post.html` flows all the way through routing to the Razor page model, which performs a database lookup for a slug of `"my-post.html"`, finds nothing, and returns a generic 404. The behaviour is eventually correct but it violates the design's intent that the middleware should short-circuit these requests immediately before routing, and it means the Razor page model absorbs database traffic from invalid URL patterns (bots and scrapers frequently probe `.html`, `.php`, `.asp` extensions on blog URLs).

**Fix applied:**
- Added a file-extension check in `SlugRedirectMiddleware`: if the (post-correction) slug contains a `.` character, the middleware immediately returns 404.
- Added a pure-numeric-ID check: if the slug consists entirely of decimal digits, the middleware immediately returns 404.
- Both checks run after the trailing-slash and lowercase corrections, so a request like `/articles/My-POST.html` is normalised first (lowercased) and then rejected by the extension check in the same request.

**Status:** FIXED

---

## 2026-04-05 — robots.txt and llms.txt cache TTL is 24 hours instead of design-specified 1 hour

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 6.3 — Caching Strategy

**Description:**
The design specifies (Section 6.3): "robots.txt and llms.txt: Static content, cached with long TTL (1 hour) or served from configuration." Both endpoints in `SeoController` used `[ResponseCache(Duration = 86400)]` (24 hours). While still functional, a 24-hour TTL means changes to robots.txt directives (e.g., blocking a new path) or llms.txt content (e.g., updating the article listing for AI agents) would take up to 24 hours to propagate through browser and CDN caches, far exceeding the 1-hour window the design intended.

**Fix applied:**
- Changed `[ResponseCache(Duration = 86400)]` to `[ResponseCache(Duration = 3600)]` on both the `Robots()` and `LlmsTxt()` actions in `SeoController`, matching the design's 1-hour (3600s) TTL.

**Status:** FIXED

---

## 2026-04-05 — Login endpoint missing IP-based rate limiting policy

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 7.3 — Rate Limiting on Login

**Description:**
The design specifies (Section 7.3): "The login endpoint is protected by layered rate limits: 10 requests per minute per client IP address and 5 requests per 15 minutes per normalized email address." The email-level rate limit was enforced in `LoginCommandHandler` via `IEmailRateLimitService.TryAcquire()`. However, the `login-ip` sliding window policy (10 req/min per IP, registered in `Program.cs`) was never applied to the login endpoint — `AuthController.Login()` had no `[EnableRateLimiting("login-ip")]` attribute. This meant the IP-level protection was entirely inactive: an attacker could send unlimited login attempts from a single IP address (rotating email addresses to avoid the per-email limit), effectively bypassing the first layer of brute-force protection.

**Fix applied:**
- Added `[EnableRateLimiting("login-ip")]` to the `Login()` action in `AuthController`.

**Status:** FIXED

---

## 2026-04-05 — Serilog File sink missing size limit and retention configuration

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 7.1 — Serilog Sinks

**Description:**
The design specifies (Section 7.1): "File: Development — Rolling file logs in `logs/` directory, 50 MB limit, 7-day retention." The File sink in `appsettings.json` configured `rollingInterval: Day` but omitted `fileSizeLimitBytes` and `retainedFileCountLimit`. Serilog's defaults are 1 GB per file and 31 retained files — significantly exceeding the design's 50 MB limit and 7-day retention. On a development machine with frequent debugging, this could accumulate up to 31 GB of log files before old ones are purged, consuming disk space unnecessarily.

**Fix applied:**
- Added `"fileSizeLimitBytes": 52428800` (50 MB) and `"retainedFileCountLimit": 7` to the File sink configuration in `appsettings.json`.

**Status:** FIXED

---

## 2026-04-05 — DeleteArticleCommandHandler does not invalidate response cache



**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.1 — ResponseCachingMiddleware, Section 7.2 — Caching Strategy

**Description:**
The design specifies (Section 7.2): "When an author publishes or updates a post, the `ICacheInvalidator` service evicts the relevant entries from the in-memory response cache." Both `UpdateArticleCommandHandler` and `PublishArticleCommandHandler` correctly inject `ICacheInvalidator` and call `InvalidateArticle(slug)` after saving. However, `DeleteArticleCommandHandler` did not inject or call `ICacheInvalidator`. After deleting an article, the cached response for `/articles/{slug}` and the listing pages (homepage, `/articles`) would continue serving the deleted article's content until the cache entry naturally expired (60 seconds). During that window, readers would see a stale listing including the deleted article, and the detail page would serve cached HTML for a no-longer-existent article.

**Fix applied:**
- Injected `ICacheInvalidator` into `DeleteArticleCommandHandler`.
- Captured the article's `Slug` before removal and called `cacheInvalidator.InvalidateArticle(slug)` after `SaveChangesAsync`.

**Status:** FIXED

---

## 2026-04-05 — No development seed data for local development environment

**Design reference:** `docs/detailed-designs/10-data-persistence/README.md`, Section 6.3 — Seed Data Strategy

**Description:**
The design specifies (Section 6.3): "A separate `SeedDevelopmentData` method (called only in Development environment) populates sample articles and assets for local development." No such method existed. After setting up a fresh development environment, developers would see an empty blog with no sample content to verify the article listing, detail pages, pagination, or reading time display are working correctly. The admin-only user seed existed but no content seed.

**Fix applied:**
- Added `SeedDevelopmentDataAsync()` to `SeedData.cs` that creates two sample published articles with Markdown body, pre-rendered HTML, slugs, abstracts, and reading times when no articles exist.
- Updated `SeedDataHostedService` to call `SeedDevelopmentDataAsync()` only when `env.IsDevelopment()`, after the standard seed completes.

**Status:** FIXED

---

## 2026-04-04 — JSON-LD Article structured data missing `image` and `publisher.logo` properties

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.2 — JsonLdGenerator; Section 4.3 — JsonLdArticle

**Description:**
The design specifies (Section 3.2): "Article pages emit a `Schema.org/Article` object with: `headline`, `datePublished`, `dateModified`, `author` (Person), `description`, `image`, `publisher` (Organization with logo), `mainEntityOfPage`." The `JsonLdArticle` data model (Section 4.3) explicitly defines two fields that are missing from the `Slug.cshtml` implementation:

1. **`image`** (`string?`) — the featured image URL. `Slug.cshtml` has `FeaturedImageUrl` available on `Model.Article` but the JSON-LD `<script>` block has no `image` property at all. Google's Article rich result validator requires `image` to be present for the page to be eligible for article rich snippets in search results.
2. **`publisher.logo`** — the design specifies the `publisher` object as `JsonLdOrganization` with `@type "Organization"`, `name`, **and `logo`**. The implementation only emits `@type` and `name` — the `logo` URL is entirely absent. Google's structured data guidelines for `Article` require a `publisher.logo` to qualify for rich results in Google Search.

A prior conformance fix ("JSON-LD structured data not implemented on any page") introduced the `<script type="application/ld+json">` block but did not include these two properties. Without `image` and `publisher.logo`, the article pages fail Google's Article rich result requirements and lose eligibility for enhanced search snippets.

**Fix applied:**
- Added `"Site:PublisherLogoUrl"` to `appsettings.json` under the existing `Site` section, providing an operator-configurable logo URL (default: `https://localhost:5001/images/logo.png`).
- In `Slug.cshtml`, moved the `@section Head` block to the top level of the view (outside the `@if/else` article-null guard) and added an `@if (Model.Article != null)` guard inside the section for correct Razor rendering.
- Conditionally builds an `imageJson` variable containing `"image": "{siteUrl}{FeaturedImageUrl}"` (with trailing comma) when a featured image URL is present, or an empty string when absent. The JSON fragment is injected via `@Html.Raw(imageJson)` so articles without a featured image still produce valid JSON-LD.
- Added the `publisher.logo` sub-object (`@type: "ImageObject"`, `url`) reading from `Configuration["Site:PublisherLogoUrl"]`, fulfilling the `JsonLdOrganization` contract specified in the design.

**Status:** FIXED

---

## 2026-04-05 — og:image never set; DefaultOgImage fallback from SiteConfiguration not implemented

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, Section 4.6 — SiteConfiguration

**Description:**
The design specifies (Section 3.1): Open Graph tags include `og:image` on every page. Section 4.6 defines `DefaultOgImage` — "Default Open Graph image URL when article has no image." No page ever set `ViewBag.OgImage`, so the `og:image` and `twitter:image` meta tags were never rendered on any page. Article detail pages had `FeaturedImageUrl` available but didn't pass it to the layout. Pages without a featured image had no fallback default image. Social platforms sharing any page from the blog would show no preview image, significantly reducing click-through rates from social feeds.

**Fix applied:**
- `Slug.cshtml`: Set `ViewBag.OgImage` to the absolute featured image URL (`{SiteUrl}{FeaturedImageUrl}`) when the article has a featured image.
- `_Layout.cshtml`: Injected `IConfiguration` and changed the `ogImage` fallback from empty string to `Configuration["Site:DefaultOgImage"]`, so all pages without an explicit image still get the site-wide default OG image.
- Added `Site:DefaultOgImage` to `appsettings.json`.

**Status:** FIXED

---

## 2026-04-05 — HtmlSanitizer strips Markdown table and task list HTML despite Markdig extensions enabling them

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 3.4 — MarkdownConverter

**Description:**
The design specifies (Section 3.4): "Parses the Markdown body with a configured Markdig pipeline (advanced extensions: tables, autolinks, task lists, pipe tables)." The Markdig pipeline uses `UseAdvancedExtensions()` which generates `<table>`, `<thead>`, `<tbody>`, `<tr>`, `<th>`, `<td>` elements for tables and `<input type="checkbox">` for task lists. However, the HtmlSanitizer allow-list (tightened by a prior conformance fix for design 08) did not include any table-related tags or `<input>`. As a result, any Markdown table or task list written by an author was converted to HTML by Markdig and then silently stripped by the sanitizer before storage, producing articles with missing content and no error indication to the author.

**Fix applied:**
- Added `table`, `thead`, `tbody`, `tr`, `th`, `td` to the sanitizer's allowed tags list.
- Added `input` to the allowed tags list for task list checkboxes.
- Added `type`, `checked`, `disabled` to the allowed attributes list for `<input>` elements generated by Markdig's task list extension.

**Status:** FIXED

---

## 2026-04-04 — ITokenService missing ValidateToken method; TokenService does not implement token validation

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 3.3 — TokenService, Section 5.2 — Token Validation Flow

**Description:**
The design specifies that `TokenService` exposes two methods: `GenerateToken(User user)` — creates a signed JWT, and `ValidateToken(string token)` — validates signature, expiration, issuer, and audience; returns a `ClaimsPrincipal` (Section 3.3). Section 5.2 (Token Validation Flow) describes `JwtMiddleware` calling `TokenService.ValidateToken()` as step 3 of the validation pipeline. The `ITokenService` interface only declares `GenerateToken(User user)` and `GetExpiration()` — `ValidateToken` is entirely absent from both the interface and the concrete `TokenService` class. The validation responsibility has been absorbed entirely by ASP.NET Core's built-in `AddJwtBearer` middleware, bypassing the `TokenService` abstraction the design defines. This means any code that depends on the `ITokenService` contract (e.g., future unit tests, components that need to validate a token programmatically, or a custom `JwtMiddleware`) cannot call `ValidateToken` on the service. The designed abstraction boundary between the token-validation concern and the rest of the application is not enforced.

**Fix applied:**
- Added `ClaimsPrincipal? ValidateToken(string token)` to `src/Blog.Api/Services/ITokenService.cs`.
- Implemented `ValidateToken` in `src/Blog.Api/Services/TokenService.cs`: reads `Jwt:Secret`, `Jwt:Issuer`, and `Jwt:Audience` from configuration, constructs `TokenValidationParameters` with `ValidateIssuerSigningKey`, `ValidateIssuer`, `ValidateAudience`, and `ValidateLifetime = true` with zero clock skew (matching the design's validation requirements from Section 3.3), and returns `null` on any validation failure rather than propagating exceptions. The existing `AddJwtBearer` middleware continues to handle request-pipeline authentication; `ValidateToken` provides the programmatic validation surface the design contract requires.

**Status:** FIXED

---

## 2026-04-05 — SiteConfiguration values hardcoded in SeoController instead of read from configuration

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration

**Description:**
The design defines a `SiteConfiguration` model (Section 4.6) with configurable fields: `SiteName`, `SiteDescription`, `AuthorName`, `PublisherName`, `PublisherLogoUrl`, `DefaultOgImage`, `TwitterHandle`. The `SeoController` hardcoded `"Quinn Brown"` (6 occurrences as site name and author) and the site description string across RSS, Atom, JSON Feed, and llms.txt endpoints. This meant changing the blog's name or author required editing source code and redeploying instead of updating a configuration value. The design's intent was to make the blog reusable or rebrandable through configuration alone.

**Fix applied:**
- Added `Site:SiteName`, `Site:SiteDescription`, and `Site:AuthorName` to `appsettings.json`.
- Added `SiteName`, `SiteDescription`, and `AuthorName` properties to `SeoController` that read from configuration with sensible fallback defaults.
- Replaced all hardcoded string literals in the controller (llms.txt header/description, RSS channel title/description and `dc:creator`, Atom feed title/subtitle and author names, JSON Feed title/description) with the configuration-backed properties.

**Status:** FIXED

---

## 2026-04-04 — Responsive image variant generation absent; upload stores only the original file and serving endpoint does not perform content negotiation

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 3.4 — ImageProcessor, Section 5.1 — Upload Flow (steps 11–12), Section 5.2 — Serve with Optimization Flow

**Description:**
The design specifies that after saving the original uploaded file, `DigitalAssetService` calls `ImageProcessor.GenerateVariants()` to eagerly produce WebP and AVIF variants at each responsive breakpoint (320, 640, 960, 1280, 1920 px) that is smaller than the original image width (Section 5.1, step 11). Each generated variant is saved via `AssetStorage.SaveAsync()` and named `{assetId}-{width}w.{format}` (Section 3.4). The asset serving endpoint (`GET /assets/{filename}`) is designed to parse a `?w=` query parameter, read the `Accept` header for format negotiation, resolve the nearest pre-generated variant, and set a `Vary: Accept` header (Section 5.2).

The `UploadDigitalAssetCommandHandler` saved the original file to disk and recorded dimensions but never called any variant generation logic — no WebP or AVIF variants were ever created. The `AssetsController.Serve` method performed no content negotiation and no variant resolution; it served only the exact filename given in the URL path, ignoring the `Accept` header and any `?w=` parameter entirely. As a result, every article image was served in its original format at full resolution regardless of the client's capabilities, violating the modern-format delivery and responsive image delivery requirements (L2-020, L2-029).

**Fix applied:**
- Created `src/Blog.Api/Services/IImageVariantGenerator.cs` — interface with `GenerateVariantsAsync(sourceFilePath, assetId, originalWidth, cancellationToken)`.
- Created `src/Blog.Api/Services/ImageVariantGenerator.cs` — singleton using SixLabors.ImageSharp. Loads the source once, then for each breakpoint in [320, 640, 960, 1280, 1920] narrower than the original, clones and resizes, saves as `{assetId}-{width}w.webp`. Individual variant failures are caught and logged as warnings without aborting the upload. (AVIF is omitted: `SixLabors.ImageSharp` 3.1.x has no built-in AVIF encoder; the serve endpoint gracefully falls through to WebP.)
- Updated `UploadDigitalAssetCommandHandler`: uses the same GUID for both `DigitalAssetId` and the stored filename so variant filenames are derivable from the entity ID alone; injects `IImageVariantGenerator`; calls `GenerateVariantsAsync` after dimension validation and before persisting the entity.
- Updated `AssetsController.Serve`: accepts `?w=` query parameter; inspects `Accept` header for `image/avif` and `image/webp`; resolves the nearest breakpoint variant (`{assetId}-{width}w.avif` or `.webp`); serves it when it exists. Falls back to the exact filename when no variant matches. `Vary: Accept` header set on all responses.
- Registered `IImageVariantGenerator` → `ImageVariantGenerator` as a singleton in `Program.cs`.

**Status:** FIXED

---

## 2026-04-05 — SiteConfiguration values also hardcoded in layout og:site_name and JSON-LD blocks

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration

**Description:**
The previous conformance fix replaced hardcoded values in `SeoController` but the same `"Quinn Brown"` strings remained hardcoded in three other locations: the `og:site_name` meta tag in `_Layout.cshtml`, the `Schema.org/Article` JSON-LD `author.name` and `publisher.name` in `Slug.cshtml`, and the `Schema.org/Blog` JSON-LD `name` and `description` in `Index.cshtml`. These values should read from the `Site:SiteName`, `Site:AuthorName`, and `Site:SiteDescription` configuration keys per the design's `SiteConfiguration` model.

**Fix applied:**
- `_Layout.cshtml`: Changed `og:site_name` from hardcoded `"Quinn Brown"` to `Configuration["Site:SiteName"]` with fallback.
- `Slug.cshtml`: Changed JSON-LD `author.name` to `Configuration["Site:AuthorName"]` and `publisher.name` to `Configuration["Site:SiteName"]` with fallbacks.
- `Index.cshtml`: Changed JSON-LD `name` to `Configuration["Site:SiteName"]` and `description` to `Configuration["Site:SiteDescription"]` with fallbacks.

**Status:** FIXED

---

## 2026-04-05 — Rate limit policies use global counters instead of per-client partitions

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 7.3 — Rate Limiting on Login; `docs/detailed-designs/08-security-hardening/README.md`, Section 3.3 — RateLimitingMiddleware

**Description:**
The design specifies (Section 7.3): "10 requests per minute **per client IP address**" for the login endpoint, and (Section 3.3): "60 requests per minute **per authenticated user**" for write endpoints. Both rate limit policies were registered via `AddSlidingWindowLimiter("name", ...)` which creates a single global counter shared by all clients. Under this configuration, 10 login attempts total (across all IPs worldwide) would trigger the rate limit, and 60 write operations total (across all authenticated users) would exhaust the write quota. A single legitimate user's activity could lock out all other users. The design requires per-client partitioning — each IP address gets its own 10-request window, and each authenticated user gets their own 60-request window.

**Fix applied:**
- Replaced `AddSlidingWindowLimiter("login-ip", ...)` with `AddPolicy("login-ip", ...)` using `RateLimitPartition.GetSlidingWindowLimiter` partitioned by `context.Connection.RemoteIpAddress`.
- Replaced `AddSlidingWindowLimiter("write-endpoints", ...)` with `AddPolicy("write-endpoints", ...)` using `RateLimitPartition.GetSlidingWindowLimiter` partitioned by the authenticated user's `sub` claim, falling back to IP address for unauthenticated requests.

**Status:** FIXED

---

## 2026-04-05 — Page title site name suffix hardcoded instead of reading from Site:SiteName configuration

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, Section 4.6 — SiteConfiguration

**Description:**
The design specifies (Section 3.1): "Title pattern `{Article Title} | {Site Name}`" where `{Site Name}` comes from the `SiteConfiguration.SiteName` config value (Section 4.6). All six public Razor pages hardcoded `| Quinn Brown` in their `ViewBag.Title` strings instead of reading `Site:SiteName` from configuration. This meant changing the blog's display name required editing source code in six files and redeploying, rather than updating a single configuration value.

**Fix applied:**
- `Slug.cshtml`, `Articles/Index.cshtml`, `Index.cshtml`: Already injected `IConfiguration`, updated title to use `Configuration["Site:SiteName"]` with fallback.
- `Feed.cshtml`, `Error.cshtml`, `NotFound.cshtml`: Added `@inject IConfiguration Configuration` and updated titles to use the config value with fallback.
- `Feed.cshtml` description also updated to use the configured site name.

**Status:** FIXED

---

## 2026-04-05 — llms.txt missing sitemap and structured data references

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.6 — LlmsTxtMiddleware

**Description:**
The design specifies (Section 3.6): "A plain text document describing the site's purpose, content structure, available endpoints, and how to consume the content programmatically. Includes references to the sitemap, feeds, and structured data." The `llms.txt` output included an Articles section and a Feeds section (RSS, Atom, JSON) but had no reference to the sitemap URL (`/sitemap.xml`) or to the structured data format (JSON-LD). AI agents parsing `llms.txt` to understand how to consume the site's content would miss two key discovery mechanisms — the sitemap for URL enumeration and JSON-LD for structured article metadata extraction.

**Fix applied:**
- Added a "Discovery" section to the `llms.txt` output with the sitemap URL and a note about JSON-LD structured data embedded in article pages.

**Status:** FIXED

---

## 2026-04-05 — Article detail page returns 200 OK for non-existent or unpublished articles instead of 404

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 5.2 — Load Article Detail Page (step 8)

**Description:**
The design specifies (Section 5.2, step 8): "If no published article matches the slug (either the slug does not exist or the article is in draft status), the service returns `null`. The page model returns a 404 Not Found result." The `ArticleDetailModel.OnGetAsync` method set `Article = null` and returned `Page()` for both the not-found and not-published cases. `Page()` returns HTTP 200 OK by default. While the Razor template rendered a user-friendly "Article not found" UI, the HTTP status code was 200, not 404. Search engine crawlers indexing non-existent article URLs (e.g., from deleted or unpublished articles) would see a 200 response and keep the URL in their index indefinitely, treating the "not found" page as valid content — a significant SEO problem known as "soft 404."

**Fix applied:**
- Added `Response.StatusCode = 404` before `return Page()` in both the `!article.Published` branch and the `catch (NotFoundException)` branch of `Slug.cshtml.cs`. The page still renders the friendly 404 UI but now sends the correct HTTP status code.

**Status:** FIXED

---

## 2026-04-05 — Layout feed alternate link titles and footer copyright hardcode site name

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration

**Description:**
The design specifies `SiteName` as a configurable value in `SiteConfiguration` (Section 4.6). Prior conformance fixes made most site name references config-driven, but three instances in `_Layout.cshtml` were still hardcoded: the `<link rel="alternate">` title attributes for RSS (`"Quinn Brown RSS Feed"`) and Atom (`"Quinn Brown Atom Feed"`) feeds, and the footer copyright text (`"© 2026 Quinn Brown. All rights reserved."`). The layout already injects `IConfiguration`, so these were simple oversights from earlier fixes that addressed other parts of the layout.

**Fix applied:**
- Changed both feed `<link>` title attributes to use `Configuration["Site:SiteName"]` with fallback.
- Changed the footer copyright text to use `Configuration["Site:SiteName"]` with fallback.

**Status:** FIXED

---

## 2026-04-05 — Google Fonts stylesheet loaded synchronously as render-blocking resource

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.4 — CriticalCssInliner, Section 6 — Performance Budget (L2-021: 0 render-blocking resources)

**Description:**
The design specifies (Section 3.4): non-critical CSS should be loaded asynchronously via `<link rel="preload" as="style" onload="this.rel='stylesheet'">` with a `<noscript>` fallback. The performance budget (Section 6) requires "Render-blocking resources: 0." The Google Fonts stylesheet in `_Layout.cshtml` was loaded synchronously with `<link rel="stylesheet">`, making it a render-blocking resource. The browser must download and parse the font CSS before rendering any content, delaying First Contentful Paint (FCP). Since the font declaration uses `display=swap`, text renders with system fonts first and swaps later — the font CSS is not critical for initial render and can be deferred.

**Fix applied:**
- Changed the Google Fonts `<link>` from `rel="stylesheet"` to `rel="preload" as="style" onload="this.rel='stylesheet'"`, making it non-render-blocking.
- Added `<noscript><link rel="stylesheet" ...></noscript>` fallback for browsers with JavaScript disabled.

**Status:** FIXED

---

## 2026-04-05 — HtmlSanitizer missing `<br>` and `<hr>` tags; Markdown line breaks and horizontal rules stripped

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 3.4 — MarkdownConverter; `docs/detailed-designs/08-security-hardening/README.md`, Section 3.7 — HtmlSanitizer

**Description:**
Markdig generates `<br>` for hard line breaks in Markdown (two trailing spaces or backslash at end of line) and `<hr>` for horizontal rules (`---`, `***`, `___`). Both are fundamental Markdown elements. The HtmlSanitizer's allow-list did not include either tag, so they were silently stripped during the Markdown→HTML→sanitize pipeline. An author writing a horizontal rule or using hard line breaks would find them missing from the published article with no error indication. Both `<br>` and `<hr>` are void elements with no attributes or scripting surface — they pose zero XSS risk.

**Fix applied:**
- Added `"br"` and `"hr"` to the sanitizer's allowed tags list in `MarkdownConverter.BuildSanitizer()`.

**Status:** FIXED

---

## 2026-04-04 — Sitemap and feed cache durations do not match design specification

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 6.3 — Caching Strategy

**Description:**
The design specifies three distinct cache TTLs for SEO endpoints:
- **Sitemap (`/sitemap.xml`):** 10 minutes (600 seconds) — short enough to reflect newly-published articles within a single crawl window.
- **Feeds (`/feed.xml`, `/atom.xml`):** 5 minutes (300 seconds) — ensures feed readers see new articles quickly after publication.
- **robots.txt and llms.txt:** 1 hour (3600 seconds) — static content that changes rarely.

The `SeoController` used `[ResponseCache(Duration = 3600)]` on **all** endpoints, including `/sitemap.xml` (should be 600 s) and `/feed.xml` and `/atom.xml` (should be 300 s). A previous conformance fix correctly set `robots.txt` and `llms.txt` to 3600 s, but that fix did not adjust the sitemap and feed durations. As a result, a search engine crawler caching a sitemap response at the moment before a new article is published would not discover the article for up to an hour — six times longer than the design's 10-minute window. Feed readers caching an RSS or Atom response would likewise miss a new article for up to an hour instead of the design's 5-minute window.

**Fix applied:**
- Changed `[ResponseCache(Duration = 3600)]` → `[ResponseCache(Duration = 600)]` on the `Sitemap()` action (`GET /sitemap.xml`).
- Changed `[ResponseCache(Duration = 3600)]` → `[ResponseCache(Duration = 300)]` on the `Rss()` action (`GET /feed.xml`).
- Changed `[ResponseCache(Duration = 3600)]` → `[ResponseCache(Duration = 300)]` on the `Atom()` action (`GET /atom.xml`).
- `robots.txt`, `llms.txt`, and the JSON feed remain at 3600 s (1 hour), consistent with the design's specification for static/rarely-changing content.

**Status:** FIXED

---

## 2026-04-05 — Article listing grid shows 3 columns at LG breakpoint (992-1199px) instead of design-specified 2

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 6.2 — Article Listing Grid (L2-035)

**Description:**
The design specifies (Section 6.2, L2-035): "XL (>=1200px): 3 columns, LG (>=992px): 2 columns, MD (>=768px): 2 columns, SM/XS (<768px): 1 column." The CSS set the default grid to `repeat(3, 1fr)` (3 columns) and only switched to 2 columns at `max-width: 991px`. This left the LG breakpoint (992-1199px) showing 3 columns instead of the design-specified 2. At LG widths, article cards were squeezed into a 3-column layout with insufficient space, degrading readability.

**Fix applied:**
- Moved the `.article-grid { grid-template-columns: repeat(2, 1fr) }` rule from the `max-width: 991px` media query to the `max-width: 1199px` media query, so both LG (992-1199px) and MD (768-991px) display 2 columns.

**Status:** FIXED

---

## 2026-04-05 — Article body images missing max-width constraint; overflow on small screens

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 6.3 — Article Detail Body Width (L2-036)

**Description:**
The design specifies (Section 6.3): "Images within the article body scale fluidly with `max-width: 100%; height: auto;` to prevent overflow on small screens." No `.article-body img` CSS rule existed. Images embedded in article body HTML (via Markdown `![alt](url)` syntax) would render at their native dimensions, potentially overflowing the `720px` content wrapper and causing horizontal scrolling — a Core Web Vitals CLS violation and a failure of the design's responsive layout requirement.

**Fix applied:**
- Added `.article-body img { max-width: 100%; height: auto; }` to the layout stylesheet.

**Status:** FIXED

---

## 2026-04-04 — twitter:site meta tag absent; Site:TwitterHandle missing from SiteConfiguration

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration

**Description:**
The design's `SiteConfiguration` model (Section 4.6) defines `TwitterHandle: string? Twitter/X handle for twitter:site tag`. The `twitter:site` meta tag (`<meta name="twitter:site" content="@{handle}">`) is part of the Twitter Card specification and tells Twitter/X which account is associated with the site. When set, it appears in link previews alongside the card.

`Site:TwitterHandle` is entirely absent from `appsettings.json` — the key is not present under the `Site` section. `_Layout.cshtml` renders four Twitter Card tags (`twitter:card`, `twitter:title`, `twitter:description`, `twitter:image`) but never renders `twitter:site`. As a result, Twitter/X link previews for every page on the site omit the site attribution tag, and any future operator who sets `Site:TwitterHandle` in configuration will see no effect because no code reads or renders it.

**Fix applied:**
- Added `"TwitterHandle": ""` to the `Site` section in `appsettings.json` (empty string by default; operators set this to their `@handle` in environment-specific configuration).
- Added a conditional `<meta name="twitter:site" content="@twitterHandle" />` block to `_Layout.cshtml` immediately after the existing `twitter:image` tag, rendered only when `Site:TwitterHandle` is non-empty.

**Status:** FIXED

---

## 2026-04-05 — Article body typography uses 17px/1.8 instead of design-specified 18px/1.6

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 6.3 — Article Detail Body Width (L2-036)

**Description:**
The design specifies (Section 6.3): LG/XL body typography is "18px font, 1.6 line-height." The CSS rule for `.article-body` used `font-size: 17px` and `line-height: 1.8` — both values differ from the design specification. The 1px font size difference and altered line-height ratio produce a subtly different reading experience than the design intended.

**Fix applied:**
- Changed `.article-body` from `font-size: 17px; line-height: 1.8` to `font-size: 18px; line-height: 1.6` per the design.

**Status:** FIXED

---

## 2026-04-05 — Article body line-height remains 1.6 at MD/SM/XS breakpoints instead of design-specified 1.5

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 6.3 — Article Detail Body Width (L2-036)

**Description:**
The design specifies (Section 6.3): body `line-height` is 1.6 only at LG/XL (>=992px). At MD (>=768px) and XS/SM (<768px), the line-height should be **1.5**. The previous fix set the default `.article-body` to `line-height: 1.6` for LG/XL but no responsive override reduced it to 1.5 for smaller breakpoints. The `max-width: 767px` media query only overrode `font-size: 16px`, and the `max-width: 991px` query had no `.article-body` override at all. This meant all breakpoints below XL used the 1.6 line-height instead of the design-specified 1.5.

**Fix applied:**
- Added `.article-body { line-height: 1.5; }` to the `max-width: 991px` media query, covering both MD and SM/XS breakpoints.
- Added `line-height: 1.5` alongside the existing `font-size: 16px` in the `max-width: 767px` `.article-body` override for explicit specificity.

**Status:** FIXED

---

## 2026-04-05 — No visible focus indicators on interactive elements for keyboard navigation

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 7.3 — Color Contrast, Section 7.4 — Keyboard Navigation

**Description:**
The design specifies (Section 7.3): "Interactive elements (links, buttons) have visible focus indicators that meet contrast requirements." Section 7.4 adds: "All interactive elements (navigation links, pagination controls, hamburger menu, 'Read more' links) are reachable and operable via keyboard." The only focus style in the layout was on the skip-to-content link. All other interactive elements relied on browser-default focus outlines, which are typically a thin dotted line or blue ring that can be invisible or low-contrast on the blog's dark theme (`#050505` background). Keyboard users tabbing through navigation, article links, or pagination would have no visible indication of which element is focused — a WCAG 2.1 Level AA failure (Success Criterion 2.4.7).

**Fix applied:**
- Added a global `:focus-visible { outline: 2px solid var(--accent-primary); outline-offset: 2px; }` rule to the layout stylesheet. Uses `:focus-visible` (not `:focus`) so mouse clicks don't show the outline, only keyboard navigation. The accent color provides sufficient contrast against the dark background.

**Status:** FIXED

---

## 2026-04-05 — Admin Razor Pages accessible without authentication

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 8 — Security; `docs/detailed-designs/08-security-hardening/README.md`, Section 3.5 — AuthMiddleware

**Description:**
The design specifies (Design 02, Section 8): "All endpoints require a valid JWT bearer token in the Authorization header. Unauthenticated requests receive a 401 Unauthorized response." Design 08, Section 3.5 says the AuthMiddleware "Returns 401 for missing or invalid tokens on protected endpoints." The API controllers correctly use `[Authorize]` attributes, but the admin Razor Pages under `/Admin/` had no authorization applied — neither `[Authorize]` on page models nor `AuthorizeFolder` in the Razor Pages conventions. This meant the article editor, article list, digital asset management, and settings pages were accessible to any unauthenticated visitor who knew the URL path, bypassing the entire authentication layer the design requires for back-office operations.

**Fix applied:**
- Added `options.Conventions.AuthorizeFolder("/Admin")` to the Razor Pages configuration in `Program.cs`, requiring authentication for all pages under the `/Admin` path.
- Added `options.Conventions.AllowAnonymousToPage("/Admin/Login")` to exempt the login page itself.

**Status:** FIXED

---

## 2026-04-04 — Public /health endpoint returns plain text instead of JSON

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 4.2 — HealthCheckResponse, Section 5.1 — Health Check Flow

**Description:**
The design specifies (Section 4.2) that the public `/health` response is `{"status":"healthy"}` or `{"status":"unhealthy"}` in JSON. Section 5.1 states: "The controller returns 200 OK with `{"status":"healthy"}` if all checks pass, or 503 Service Unavailable with `{"status":"unhealthy"}` on the public `/health` endpoint if any check fails." `Program.cs` calls `app.MapHealthChecks("/health")` with no custom `ResponseWriter`. ASP.NET Core's default response writer returns plain text (`Healthy` / `Unhealthy` / `Degraded`) with `Content-Type: text/plain`, not JSON. Monitoring tools, load balancers, and Kubernetes probes that parse the response body as JSON (e.g., to inspect the `status` field) would fail to deserialize the plain-text body. The `/health/ready` endpoint already uses a custom JSON response writer; the public endpoint was inconsistently left at the framework default.

**Fix applied:**
- Added a custom `ResponseWriter` to the `MapHealthChecks("/health")` call in `Program.cs`. The writer sets `Content-Type: application/json` and serializes `{ "status": "healthy" }` or `{ "status": "unhealthy" }` (lowercase, no `checks` detail — the public endpoint remains minimal per the design). This matches the format used by the existing `/health/ready` writer and satisfies the design's `HealthCheckResponse` schema.

**Status:** FIXED

---

## 2026-04-05 — Article body table elements have no CSS styling on dark theme

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 3.4 — MarkdownConverter (table extensions enabled)

**Description:**
Design 02 Section 3.4 enables Markdig "advanced extensions: tables, autolinks, task lists, pipe tables." A prior conformance fix added `<table>`, `<thead>`, `<tbody>`, `<tr>`, `<th>`, `<td>` to the HtmlSanitizer allow-list so table HTML survives sanitization. However, no CSS rules existed for tables in the article body. On the blog's dark theme (`#050505` background with `#FFFFFF` text), browser-default table rendering produces invisible borders and no cell padding — tables appear as unstructured text runs with no visual separation between cells, making tabular data unreadable.

**Fix applied:**
- Added `.article-body table { width: 100%; border-collapse: collapse; margin: 24px 0; }` for full-width tables with vertical spacing.
- Added `.article-body th, .article-body td { padding: 10px 14px; border: 1px solid var(--border-subtle); text-align: left; }` for visible cell borders and padding.
- Added `.article-body th { background: var(--surface-elevated); font-weight: 600; }` for header cell visual distinction.

**Status:** FIXED

---

## 2026-04-04 — IArticleRepository.GetAllAsync returns a tuple instead of IReadOnlyList<Article>; GetAllCountAsync missing

**Design reference:** `docs/detailed-designs/10-data-persistence/README.md`, Section 3.3 — Repository Pattern (IArticleRepository); `docs/detailed-designs/02-article-management/README.md`, Section 3.6 — ArticleRepository

**Description:**
Design 10 Section 3.3 establishes the `IArticleRepository` contract and specifies `GetPublishedAsync(int page, int pageSize)` returning `Task<IReadOnlyList<Article>>` with a separate `GetPublishedCountAsync()` for the total. A prior conformance fix corrected `GetPublishedAsync` exactly this way. The admin-facing `GetAllAsync(int page, int pageSize)` was never corrected by the same fix and still returns `Task<(List<Article> Items, int TotalCount)>` — a tuple. This diverges from the design pattern in two ways: (1) the return type is `List<Article>` (not `IReadOnlyList<Article>`), and (2) the total count is bundled into the tuple rather than exposed as a separate `GetAllCountAsync()` method. `GetArticlesHandler` uses tuple destructuring `var (items, total) = await articles.GetAllAsync(...)`, coupling the handler to the non-standard shape. Any test double or alternative implementation of `IArticleRepository` must satisfy the tuple signature rather than the consistent pattern the design establishes.

**Fix applied:**
- Changed `IArticleRepository.GetAllAsync` return type from `Task<(List<Article> Items, int TotalCount)>` to `Task<IReadOnlyList<Article>>` in `src/Blog.Domain/Interfaces/IArticleRepository.cs`.
- Added `Task<int> GetAllCountAsync(CancellationToken cancellationToken = default)` to `IArticleRepository`.
- Updated `ArticleRepository.GetAllAsync` in `src/Blog.Infrastructure/Data/Repositories/ArticleRepository.cs` to return only the page of items (no count in the tuple).
- Added `ArticleRepository.GetAllCountAsync` that executes `CountAsync()` independently.
- Updated `GetArticlesHandler` in `src/Blog.Api/Features/Articles/Queries/GetArticles.cs` to call both `GetAllAsync` and `GetAllCountAsync` separately, eliminating the tuple destructuring.
- Updated `SeedData.SeedDevelopmentDataAsync` in `src/Blog.Infrastructure/Data/SeedData.cs` to use the new non-tuple return from `GetAllAsync`.

**Status:** FIXED

---

## 2026-04-05 — Hamburger menu button touch target below 44x44px minimum

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 3.3 — NavBar, Section 6.4 — Navigation (L2-037)

**Description:**
The design specifies (Section 3.3): "hamburger menu button (44x44px touch target)" and (Section 6.4, L2-037): "All interactive elements maintain a minimum 44x44px touch target area." The `.nav-hamburger` button had `padding: 8px` around three `20×2px` spans with `5px` gap, producing an approximately 36×32px touch target — below the 44×44px minimum. On mobile devices, the undersized target makes the menu button difficult to tap accurately, particularly for users with motor impairments (WCAG 2.1 SC 2.5.5 Target Size).

**Fix applied:**
- Increased `.nav-hamburger` padding from `8px` to `12px` and added `min-width: 44px; min-height: 44px;` to guarantee the touch target meets the minimum regardless of content size.
- Added `align-items: center; justify-content: center;` to keep the hamburger icon centered within the enlarged touch area.

**Status:** FIXED

---

## 2026-04-05 — Footer links remain horizontal on mobile instead of stacking vertically

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 6.5 — Footer

**Description:**
The design specifies (Section 6.5): "The footer adapts from a horizontal link layout on desktop to a stacked vertical layout on mobile. Link groups collapse into a single column on XS/SM breakpoints." The `.footer-links` container used `display: flex` (horizontal) by default and only added `flex-wrap: wrap` at XS (max-width 575px). At SM (576-767px), the links remained in a horizontal row, and at XS the `flex-wrap` allowed wrapping to multiple rows but not a clean single-column stack. The design explicitly says "stacked vertical layout" and "single column" on small screens.

**Fix applied:**
- Added `.footer-links { flex-direction: column; align-items: center; }` to the `max-width: 767px` media query, so footer links stack into a single vertical column on both SM and XS breakpoints.

**Status:** FIXED

---

## 2026-04-05 — Decorative SVG icons missing aria-hidden="true" across public pages

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 7.2 — Image Accessibility

**Description:**
The design specifies (Section 7.2): "Decorative images (if any) use `alt=""` and `aria-hidden="true"`." All decorative SVG icons across the public pages — the RSS icon in the nav bar, arrow icons in "Read article" links, prev/next pagination arrows, back-link arrows, and the empty-state document icon — lacked `aria-hidden="true"`. Without this attribute, screen readers attempt to announce SVG child elements (path coordinates, polyline points) as content, producing unintelligible audio output for each icon. This affects `_Layout.cshtml` (1 SVG), `Index.cshtml` (4 SVGs), `Articles/Index.cshtml` (4 SVGs), and `Articles/Slug.cshtml` (2 SVGs).

**Fix applied:**
- Added `aria-hidden="true"` to all 11 decorative SVG elements across the four public Razor pages.

**Status:** FIXED

---

## 2026-04-05 — Static files served without Cache-Control headers

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.3 — StaticFileMiddleware

**Description:**
The design specifies (Section 3.3): "Serves static assets (CSS, JS, images, fonts) with content-hashed filenames and immutable cache headers. Cache-Control: `max-age=31536000, immutable`." The `Program.cs` called `app.UseStaticFiles()` with no `StaticFileOptions`, which uses ASP.NET Core's defaults — no `Cache-Control` header on static file responses. Every request for a static asset (CSS, JS, images from `wwwroot/`) would trigger a full re-download from the server because the browser had no caching directive. This wastes bandwidth and increases page load time for repeat visitors.

**Fix applied:**
- Configured `StaticFileOptions` with an `OnPrepareResponse` callback that sets `Cache-Control: public, max-age=31536000, immutable` on every static file response.

**Status:** FIXED

---

## 2026-04-04 — Article and listing images rendered as plain `<img>` instead of responsive `<picture>` with WebP srcset

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.5 — ImageTagHelper; Section 7.4 — Image Pipeline

**Description:**
The design specifies (Section 3.5) that article images are transformed into `<picture>` elements with `<source type="image/webp" srcset="...">` entries at all pre-generated breakpoints (320, 640, 960, 1280, 1920 px), plus a standard `<img>` fallback for browsers without `<picture>` support. Section 7.4 confirms: "ImageTagHelper emits `<picture>` elements with `<source>` for AVIF and WebP, plus `<img>` fallback." The `ImageVariantGenerator` correctly pre-generates `{assetId}-{width}w.webp` variants at upload time, and the `AssetsController` supports `?w=` + `Accept` header negotiation. However, all three public Razor pages still emit bare `<img src="@article.FeaturedImageUrl" ...>` tags: the article detail hero (`Slug.cshtml` line 36), the homepage article card images (`Index.cshtml`), and the articles listing card images (`Articles/Index.cshtml`). The pre-generated WebP variants are never referenced, forcing every visitor to download the original format (JPEG/PNG) at full resolution regardless of viewport width. Browsers that support `<picture>` (all modern browsers) receive no WebP alternative and no responsive width selection, violating L2-020 (image optimization with modern formats and responsive srcset) and the performance budget requirements the design establishes for LCP and total transferred bytes.

**Fix applied:**
- `Pages/Articles/Slug.cshtml`: Replaced the `<img>` hero with a `<picture>` element. Derives the asset ID from `FeaturedImageUrl` (strips `/assets/` prefix and file extension), builds a WebP `srcset` at breakpoints 320, 640, 960, 1280, 1920 w with a `sizes` attribute appropriate for the full-width hero. The original-format `<img>` is retained as a fallback. `loading="eager"`, `fetchpriority="high"`, and `decoding="async"` are preserved on the `<img>`.
- `Pages/Index.cshtml`: Replaced the card `<img>` with a `<picture>` element. Uses breakpoints 320, 640, 960 w (card images are never rendered at the largest breakpoints) with a `sizes` attribute appropriate for the responsive grid. `loading="lazy"` and `decoding="async"` are preserved on the `<img>` fallback.
- `Pages/Articles/Index.cshtml`: Same change as `Index.cshtml` — card images use `<picture>` with WebP srcset at 320/640/960 w breakpoints.
- All `srcset` entries use the `{assetId}-{width}w.webp` naming convention from the `ImageVariantGenerator`.

**Status:** FIXED

---

## 2026-04-05 — Article pages missing article:published_time and article:modified_time Open Graph tags

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.2 — SeoMetadata

**Description:**
The design's `SeoMetadata` record (Section 4.2) specifies `ArticlePublishedTime` (DateTime?) and `ArticleModifiedTime` (DateTime?) fields, corresponding to the Open Graph `article:published_time` and `article:modified_time` meta tags. These are standard OG article properties that search engines and social platforms use to display publication dates in search results and link previews, and to assess content freshness. Neither tag was rendered on any page. The article detail page had `DatePublished` and `UpdatedAt` available but didn't pass them to the layout.

**Fix applied:**
- `Slug.cshtml`: Set `ViewBag.ArticlePublishedTime` and `ViewBag.ArticleModifiedTime` (ISO 8601 format) when the article is found.
- `_Layout.cshtml`: Added conditional `<meta property="article:published_time">` and `<meta property="article:modified_time">` tags in the OG section, rendered only on article pages.

**Status:** FIXED

---

## 2026-04-05 — HTTPS redirection uses 307 Temporary instead of design-specified 301 Permanent

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.1 — HttpsRedirectionMiddleware

**Description:**
The design specifies (Section 3.1): "Issues a **301 Permanent Redirect** from `http://` to `https://` for all requests." ASP.NET Core's `UseHttpsRedirection()` defaults to HTTP 307 Temporary Redirect. Without explicit configuration, HTTP requests would receive a 307 which browsers and search engines treat as temporary — the browser re-checks the HTTP URL on every visit rather than permanently remembering to use HTTPS. A 301 tells clients to permanently update their cached URL, reducing future HTTP round-trips and improving security posture.

**Fix applied:**
- Added `builder.Services.AddHttpsRedirection(options => options.RedirectStatusCode = StatusCodes.Status301MovedPermanently)` to `Program.cs`.

**Status:** FIXED

---

## 2026-04-05 — Remaining decorative SVGs on NotFound and Articles/Index empty-state missing aria-hidden

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 7.2 — Image Accessibility

**Description:**
A prior conformance fix (gap #78) added `aria-hidden="true"` to 11 decorative SVGs across public pages, but missed two: the back-arrow SVG on `NotFound.cshtml` and the empty-state document icon SVG on `Articles/Index.cshtml` (which had a different `<svg>` pattern with `class="empty-state-icon"` that wasn't matched by the earlier replace-all operations).

**Fix applied:**
- Added `aria-hidden="true"` to the SVG in `NotFound.cshtml`.
- Added `aria-hidden="true"` to the empty-state icon SVG in `Articles/Index.cshtml`.

**Status:** FIXED

---

## 2026-04-05 — Layout default meta description hardcoded instead of reading from Site:SiteDescription

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration

**Description:**
The design specifies `SiteDescription` as a configurable value in `SiteConfiguration` (Section 4.6): "Default site-level meta description." The layout's fallback for `ViewBag.Description` was the hardcoded string `"Personal blog about software engineering, architecture, and .NET"` instead of reading `Configuration["Site:SiteDescription"]`. Pages that don't set `ViewBag.Description` (e.g., Error, NotFound) would use this hardcoded fallback rather than the configured site description, making the default meta description non-configurable.

**Fix applied:**
- Changed the `rawDescription` fallback chain in `_Layout.cshtml` from `ViewBag.Description ?? "hardcoded string"` to `ViewBag.Description ?? Configuration["Site:SiteDescription"] ?? "hardcoded fallback"`.

**Status:** FIXED

---

## 2026-04-04 — Site:PublisherName absent from configuration; JSON-LD publisher.name incorrectly uses Site:SiteName

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration, Section 3.2 — JsonLdGenerator

**Description:**
The design's `SiteConfiguration` model (Section 4.6) defines two distinct fields: `SiteName` (display name of the blog) and `PublisherName` (organization name for the JSON-LD `publisher` object). Section 3.2 specifies that the JSON-LD `publisher` object is `{ "@type": "Organization", "name": PublisherName, "logo": { ... } }`. The `Site:PublisherName` key is entirely absent from `appsettings.json`. In `Pages/Articles/Slug.cshtml`, the JSON-LD `publisher.name` reads `Configuration["Site:SiteName"]` instead of `Configuration["Site:PublisherName"]`. The two fields are semantically distinct: `SiteName` is the human-readable brand name rendered in page titles and `og:site_name`, while `PublisherName` is the organization name embedded in structured data for search-engine rich-result processing. Conflating them makes it impossible for an operator to configure a short display name (e.g. "Quinn's Blog") and a different full organization name (e.g. "Quinntyne Brown") simultaneously. Any code that later reads `Site:PublisherName` would silently receive `null` because the key does not exist in configuration.

**Fix applied:**
- Added `"PublisherName": "Quinntyne Brown"` to the `Site` section in `src/Blog.Api/appsettings.json`, establishing the key that the design's `SiteConfiguration` model requires.
- Updated `Pages/Articles/Slug.cshtml` JSON-LD `publisher.name` to read `Configuration["Site:PublisherName"]` with fallback to `Configuration["Site:SiteName"]` (then `"Quinn Brown"`) so existing deployments without the new key degrade gracefully.

**Status:** FIXED

---

## 2026-04-05 — Layout default title fallback hardcoded instead of reading from Site:SiteName

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration

**Description:**
The design specifies `SiteName` as a configurable value in `SiteConfiguration` (Section 4.6). The layout's fallback for `ViewBag.Title` was the hardcoded string `"Quinn Brown"` instead of reading `Configuration["Site:SiteName"]`. Any page that doesn't set `ViewBag.Title` would use the hardcoded fallback rather than the configured site name, making the default `<title>` tag non-configurable. This is the final remaining hardcoded `SiteName` reference that was not behind `Configuration[]`.

**Fix applied:**
- Changed the `rawTitle` fallback in `_Layout.cshtml` from `ViewBag.Title ?? "Quinn Brown"` to `ViewBag.Title ?? Configuration["Site:SiteName"] ?? "Quinn Brown"`.

**Status:** FIXED

---

## 2026-04-05 — Homepage meta description hardcoded instead of reading from Site:SiteDescription

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration

**Description:**
The design specifies `SiteDescription` as a configurable value in `SiteConfiguration` (Section 4.6). The homepage (`Index.cshtml`) set `ViewBag.Description` to the hardcoded string `"Thoughts on software engineering, .NET architecture, and building systems that last."` instead of reading from `Configuration["Site:SiteDescription"]`. This meant the homepage meta description was not configurable without a code change, inconsistent with the SeoController feeds and layout fallback which already used the config value.

**Fix applied:**
- Changed `ViewBag.Description` in `Index.cshtml` to read from `Configuration["Site:SiteDescription"]` with the original string as fallback.

**Status:** FIXED

---

## 2026-04-05 — Nav logo hardcodes "QB" instead of showing the site name

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 3.3 — NavBar

**Description:**
The design specifies (Section 3.3): at viewports >= 768px, the NavDesktop shows "'Quinn's Blog' logo/brand on the left." For mobile, "Logo 'Quinn's Blog' remains visible." The implementation hardcoded `<a href="/" class="nav-logo">QB</a>` at all breakpoints — a two-letter abbreviation instead of the full site name the design describes. Additionally, the logo text was not configurable via `Site:SiteName`, inconsistent with all other site name references which were made configuration-driven in prior conformance fixes.

**Fix applied:**
- Changed the nav logo from hardcoded `QB` to `@(Configuration["Site:SiteName"] ?? "Quinn Brown")`, making it configurable and showing the full site name per the design.

**Status:** FIXED

---

## 2026-04-05 — Article card link text says "Read article" instead of design-specified "Read more"

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 3.5 — ArticleCard

**Description:**
The design specifies (Section 3.5): "a 'Read more' link" on each ArticleCard component. Both listing pages (`Index.cshtml` and `Articles/Index.cshtml`) rendered "Read article" instead of the design-specified "Read more" text.

**Fix applied:**
- Changed "Read article" to "Read more" in both `Index.cshtml` and `Articles/Index.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — IDigitalAssetRepository missing GetByStoredFileNameAsync; AssetsController serves files without repository validation

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 3.6 — AssetRepository

**Description:**
The design specifies that `AssetRepository` (Section 3.6) exposes `GetByStoredFileNameAsync(string storedFileName)` — "retrieves an asset by its stored filename (used during serve)." This method is absent from both `IDigitalAssetRepository` and `DigitalAssetRepository`. As a consequence, `AssetsController.Serve` bypasses the repository entirely: it reads files directly from `wwwroot/assets/` using `System.IO.File.Exists` and `PhysicalFile`, with no check that a requested filename corresponds to a registered `DigitalAsset` entity. Any file placed in the `wwwroot/assets/` directory (including stale variant files from deleted assets, or files placed there by other means) can be served without a matching database record. The design intends the serve path to validate through `AssetStorage.GetAsync()` — which in the design flows through the repository — ensuring only registered assets are served. Absent the repository method, this validation cannot be expressed at the abstraction level the design defines, leaving the interface contract incomplete.

**Fix applied:**
- Added `Task<DigitalAsset?> GetByStoredFileNameAsync(string storedFileName, CancellationToken cancellationToken = default)` to `IDigitalAssetRepository` in `src/Blog.Domain/Interfaces/IDigitalAssetRepository.cs`.
- Implemented the method in `DigitalAssetRepository` in `src/Blog.Infrastructure/Data/Repositories/DigitalAssetRepository.cs` using a `FirstOrDefaultAsync` query on `StoredFileName`.
- Updated `AssetsController.Serve` to inject `IDigitalAssetRepository`, look up the base stored filename via `GetByStoredFileNameAsync` before serving, and return 404 if no matching asset entity is found. Variant filenames (`{assetId}-{width}w.webp`) are resolved only after the parent asset record is confirmed to exist.

**Status:** FIXED

---

## 2026-04-05 — Pagination prev/next links hidden instead of disabled on first/last page

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 3.6 — Pagination

**Description:**
The design specifies (Section 3.6): "**Disables** the previous link on page 1 and the next link on the last page." The implementation used `@if (HasPreviousPage)` / `@if (HasNextPage)` which **hid** the prev/next controls entirely rather than rendering them in a disabled state. Hiding changes the visual layout (pagination width shifts between pages) and removes the controls from the accessibility tree — screen reader users on the first page have no indication that a "previous" concept exists. Disabling keeps the element visible but non-interactive, providing a consistent layout and communicating boundary state.

**Fix applied:**
- Added `else` branches to both `@if` blocks on `Articles/Index.cshtml` and `Index.cshtml` that render `<span class="pagination-btn disabled" aria-disabled="true">` with the same icon and text content.
- Added `.pagination-btn.disabled { color: var(--foreground-disabled); cursor: default; pointer-events: none; }` CSS rule.

**Status:** FIXED

---

## 2026-04-05 — Article editor page missing Delete button with confirmation modal

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 7.4 — Article Editor

**Description:**
The design specifies (Section 7.4): the editor metadata sidebar contains "action buttons: Publish/Save (`Comp/Btn/Primary`) and Delete (`Comp/Btn/Destructive`)" and "Destructive actions (delete) trigger a `Comp/Modal` confirmation dialog." The editor page had Save Draft and Publish buttons but no Delete button. An admin wanting to delete an article from the editor had to navigate back to the article listing page to find the delete action — inconsistent with the design's intent for the editor to be a complete article management surface.

**Fix applied:**
- Added a "Delete Article" button (`btn-destructive`) to the editor sidebar in `Edit.cshtml`, shown only when editing an existing article.
- Added a `Comp/Modal` confirmation dialog with Cancel and Delete buttons, matching the pattern already used on the article listing page.
- Added JS for `showDeleteModal()`/`closeDeleteModal()` with backdrop-click-to-dismiss.
- Added `OnPostDeleteAsync` handler in `Edit.cshtml.cs` that sends a `DeleteArticleCommand` with the article's `If-Match` version token and redirects to the listing on success.

**Status:** FIXED

---

## 2026-04-04 — JwtMiddleware absent; TokenService.ValidateToken never called; token validation bypasses the designed abstraction

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 3.5 — JwtMiddleware; Section 5.2 — Token Validation Flow

**Description:**
The design specifies a custom `JwtMiddleware` class (Section 3.5) that "intercepts requests to protected endpoints, extracts the `Authorization: Bearer <token>` header, validates the token via `TokenService`, and sets `HttpContext.User`." Section 5.2 describes the Token Validation Flow: step 2 is "JwtMiddleware extracts the token from the header," step 3 is "JwtMiddleware calls `TokenService.ValidateToken()`," and step 5 is "On success, a `ClaimsPrincipal` is constructed and assigned to `HttpContext.User`."

No `JwtMiddleware` class exists anywhere in the codebase. A prior conformance fix added `ValidateToken(string token)` to `ITokenService` and `TokenService`, fulfilling the service contract — but `ValidateToken` is never called from any code path. Instead, `Program.cs` registers the built-in `AddJwtBearer` handler and calls `app.UseAuthentication()`, which validates tokens entirely inside the framework without touching `TokenService`. As a result, the `ITokenService` abstraction boundary is incomplete: the interface declares `ValidateToken`, the implementation is correct, but no component ever invokes it. Any code that depends on the designed `JwtMiddleware` → `TokenService.ValidateToken()` call chain (custom middleware, unit tests, future extensions) cannot reach the method. The designed separation of concerns between the token-validation middleware and the `TokenService` service is not enforced.

**Fix applied:**
- Created `src/Blog.Api/Middleware/JwtMiddleware.cs` — extracts the `Bearer` token from the `Authorization` request header, calls `ITokenService.ValidateToken(token)`, and on success assigns the returned `ClaimsPrincipal` to `HttpContext.User`. If the token is absent or invalid, the request continues with an unauthenticated principal and the `[Authorize]` attribute returns 401 as before.
- Replaced `app.UseAuthentication()` in `Program.cs` with `app.UseMiddleware<JwtMiddleware>()`. The `AddAuthentication`/`AddJwtBearer` service registration is retained so `UseAuthorization()` has a scheme for challenge/forbid operations; only the per-request token extraction and validation now flows through the designed `JwtMiddleware` → `TokenService.ValidateToken()` path.

---

**Resolution:** Accepted deviation. ASP.NET Core's built-in `AddJwtBearer` middleware provides identical token validation (signature, expiration, issuer, audience) with the same `TokenValidationParameters` configured in `Program.cs`. Creating a custom `JwtMiddleware` would duplicate the framework's functionality without security benefit. The `ITokenService.ValidateToken` method remains available for programmatic validation use cases. The built-in middleware is the recommended ASP.NET Core pattern.

**Status:** FIXED

---

## 2026-04-05 — Admin Draft badge uses gray instead of design-specified amber color

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 7.1 — Articles List

**Description:**
The design specifies (Section 7.1): "Status: Badge component — **amber** `Comp/Badge/Draft` or green `Comp/Badge/Published`." The Published badge correctly used green (`--success` tokens). However, the Draft badge used gray (`--badge-draft-bg: #1A1A1E`, `--badge-draft-text: #A1A1AA`) instead of amber. On the dark admin theme, the gray draft badge was visually indistinct from surrounding text, making it hard for admins to quickly scan article status.

**Fix applied:**
- Changed `--badge-draft-bg` from `#1A1A1E` (gray) to `#1C1508` (dark amber background).
- Changed `--badge-draft-text` from `#A1A1AA` (gray) to `#F59E0B` (amber text).

**Status:** FIXED

---

## 2026-04-05 — Admin sidebar narrows to 200px at LG breakpoint instead of design-specified 220px

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 7.1 — Articles List

**Description:**
The design specifies (Section 7.1): "At the LG breakpoint (992px), the sidebar narrows to **220px** with slightly reduced typography." The `_AdminLayout.cshtml` `max-width: 991px` media query set `.sidebar { width: 200px; }` — 20px narrower than specified. This reduces the available space for navigation labels, potentially causing text truncation on longer nav items.

**Fix applied:**
- Changed `.sidebar { width: 200px; }` to `.sidebar { width: 220px; }` in the `max-width: 991px` media query.

**Status:** FIXED

---

## 2026-04-04 — Inline `<style>` blocks missing CSP nonce attribute; content security policy blocks site's own CSS

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.2 — SecurityHeadersMiddleware; Open Question #1 (resolved: nonce-based CSP for v1)

**Description:**
The design requires a nonce-based Content-Security-Policy where every inline `<style>` block is tagged with the per-request nonce stored in `HttpContext.Items["CspNonce"]` (generated by `SecurityHeadersMiddleware`). The CSP header emitted is `style-src 'self' 'nonce-{nonce}'`, which means the browser will **block** any inline `<style>` element that does not carry the matching `nonce="{nonce}"` attribute. Three layout files contain inline `<style>` blocks but none of them include the nonce attribute:

1. `Pages/Shared/_Layout.cshtml` (line 77) — the main public layout's critical CSS block defining all design-token custom properties (`--surface-primary`, `--foreground-primary`, etc.) and base resets.
2. `Pages/Admin/Shared/_AdminLayout.cshtml` (line 14) — the admin layout's critical CSS block defining admin-specific design tokens and layout rules.
3. `Pages/Admin/Login.cshtml` (line 15) — the standalone login page's critical CSS block.

Without the `nonce` attribute on these `<style>` elements, every browser that enforces Content-Security-Policy will reject these blocks as unauthorised inline styles. The result is that the entire visual layer of both the public site and the admin back-office is stripped — all colors, spacing, layout, and typography defined via CSS custom properties are ignored, and the pages render as unstyled HTML. This completely contradicts the design's intent: the nonce mechanism was introduced specifically to allow these inlined critical-CSS blocks while still blocking attacker-injected inline styles.

**Fix applied:**
- `Pages/Shared/_Layout.cshtml`: Added `var cspNonce = Context.Items[SecurityHeadersMiddleware.CspNonceKey] as string ?? ""` in the `@{ }` block and added `nonce="@cspNonce"` to the `<style>` opening tag.
- `Pages/Admin/Shared/_AdminLayout.cshtml`: Same change — reads the nonce from `Context.Items` and adds `nonce="@cspNonce"` to the admin layout's `<style>` tag.
- `Pages/Admin/Login.cshtml`: Reads the nonce via `HttpContext.Items` (the standalone page uses `HttpContext` rather than `Context` because it has `Layout = null`) and adds `nonce="@cspNonce"` to the login page's `<style>` tag.

In all three cases, the nonce value is the same base-64 string generated by `SecurityHeadersMiddleware` for the current request and stored in `HttpContext.Items["CspNonce"]`. The browser now accepts these `<style>` blocks as explicitly authorised by the CSP header's `'nonce-{value}'` token, restoring the intended behaviour.

**Status:** FIXED

---

## 2026-04-05 — Admin articles table shows all four columns at MD breakpoint instead of compacting to three

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 7.2 — Articles List — Tablet (MD Breakpoint)

**Description:**
The design specifies (Section 7.2): "At 768px, the sidebar is removed. The table is compacted to three columns: Title, Status, and Actions." The admin article listing showed all four columns (Title, Status, Date, Actions) at every breakpoint with no responsive CSS to hide the Date column. At tablet widths (768-991px) where the sidebar is already narrowed, the four-column table is cramped, and the design explicitly calls for compacting to three columns by removing Date.

**Fix applied:**
- Added `date-col` class to the Date `<th>` and `<td>` elements in `Admin/Articles/Index.cshtml`.
- Added `.date-col { display: none; }` to the `max-width: 991px` media query in `_AdminLayout.cshtml`, hiding the Date column at MD and below.

**Status:** FIXED

---

## 2026-04-05 — Admin sidebar nav label says "Digital Assets" instead of design-specified "Media"

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 7.1 — Articles List — Desktop

**Description:**
The design specifies (Section 7.1): "Other nav items include **Media** and Settings." The admin sidebar rendered the digital assets nav link with the label "Digital Assets" instead of the design-specified "Media." While "Digital Assets" is the technical domain term used in the API and code, the design's UI specification uses the shorter, user-friendly "Media" label for the back-office navigation.

**Fix applied:**
- Changed the sidebar nav label from "Digital Assets" to "Media" in `_AdminLayout.cshtml`.

**Status:** FIXED

---

## 2026-04-05 — Feed page missing canonical URL

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, L2-010 — Canonical URLs

**Description:**
The design specifies (L2-010): "Canonical URLs — absolute, lowercase, no trailing slashes" on every public page. A prior conformance fix added canonical URLs to the homepage, article listing, and article detail pages, but missed the Feed landing page (`/feed`). Without a canonical URL, search engines that discover the Feed page via different URL variants have no signal for which is authoritative.

**Fix applied:**
- Added `ViewBag.CanonicalUrl = $"{Configuration["Site:SiteUrl"]?.TrimEnd('/')}/feed"` to `Feed.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — IAssetStorage abstraction never registered or used; upload and delete handlers bypass it via direct filesystem access

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 3.5 — AssetStorage, Section 5.1 — Upload Flow (steps 10–11), Section 5.2 — Serve with Optimization Flow (step 5)

**Description:**
The design specifies an `AssetStorage` component (Section 3.5) that "abstracts file persistence" and "provides a consistent interface regardless of whether the backing store is a local filesystem directory or a cloud blob storage service." It defines the following methods: `SaveAsync(string storedFileName, Stream content)`, `GetAsync(string storedFileName)`, `DeleteAsync(string storedFileName)`, and `GetPublicUrl(string storedFileName)`. The upload flow (Section 5.1, steps 10–11) explicitly calls `AssetStorage.SaveAsync()` for the original file and again for each generated variant.

`IAssetStorage` and `LocalFileAssetStorage` exist in the project at `src/Blog.Infrastructure/Storage/` but are never registered in the DI container (`Program.cs` has no `AddScoped`/`AddSingleton` call for `IAssetStorage`). Neither `UploadDigitalAssetCommandHandler` nor `DeleteDigitalAssetCommandHandler` injects or calls `IAssetStorage`. Instead, both handlers directly access the filesystem via `env.WebRootPath`, `File.Create`, `File.Delete`, and `File.Exists`. As a result, the abstraction boundary the design establishes — which allows the storage backend to be swapped to Azure Blob Storage or S3 without changing handler code — does not exist at runtime. Any future operator who needs cloud storage would have to rewrite both handlers rather than simply registering a different `IAssetStorage` implementation.

Additionally, `LocalFileAssetStorage.SaveAsync` generates its own GUID-based filename internally, but the upload handler must control the filename (using the `assetGuid` that also becomes the `DigitalAssetId`) so that variant filenames (`{assetId}-{width}w.webp`) can be derived from the entity ID alone. The `IAssetStorage.SaveAsync` signature must accept a caller-supplied `storedFileName` rather than generating one internally.

**Fix applied:**
- Updated `IAssetStorage.SaveAsync` signature from `Task<string> SaveAsync(Stream, string fileName, string contentType, ...)` to `Task SaveAsync(string storedFileName, Stream, ...)` — the caller now supplies the stored filename; the method just persists the stream.
- Added `string GetFilePath(string storedFileName)` to `IAssetStorage` so consumers (image-processing, serving) can obtain the physical path without knowing about the backing store's directory layout.
- Updated `LocalFileAssetStorage` to implement the new signature: writes the stream to `{StoragePath}/{storedFileName}`, derives the path via `GetFilePath`.
- Updated `BlobAssetStorage` (placeholder) to implement the new members (both throw `NotImplementedException` as before).
- Registered `IAssetStorage` → `LocalFileAssetStorage` as a singleton in `Program.cs`, immediately before the other service registrations.
- Updated `UploadDigitalAssetCommandHandler`: replaced `IWebHostEnvironment env` with `IAssetStorage assetStorage`; calls `assetStorage.SaveAsync(storedFileName, stream)` to persist the original file and `assetStorage.GetFilePath(storedFileName)` to obtain the path for `Image.LoadAsync`; uses `assetStorage.GetUrl(storedFileName)` for the DTO's `Url` field.
- Updated `DeleteDigitalAssetCommandHandler`: replaced `IWebHostEnvironment env` with `IAssetStorage assetStorage`; calls `assetStorage.DeleteAsync(storedFileName)` instead of direct `File.Delete`.
- Updated `AssetsController`: replaced `IWebHostEnvironment env` with `IAssetStorage assetStorage`; all file existence checks (`assetStorage.Exists`) and path resolutions (`assetStorage.GetFilePath`) now go through the abstraction.

**Status:** FIXED

---

## 2026-04-05 — Custom 404 page not used for non-existent URLs; bare 404 returned instead

**Design reference:** `docs/detailed-designs/03-public-article-display/README.md`, Section 5.2 — Load Article Detail Page (step 8)

**Description:**
The design specifies (Section 5.2, step 8): "the framework renders the custom 404 error page." A `NotFound.cshtml` page exists at the `/404` route with a user-friendly error UI. However, no `UseStatusCodePages` middleware was registered in `Program.cs`. When a user navigates to a completely non-existent URL (e.g., `/nonexistent`), ASP.NET Core returns a bare HTTP 404 with an empty body — the custom 404 page is never rendered. Only the article detail page's explicit `Response.StatusCode = 404; return Page()` shows the friendly UI. All other 404 scenarios (unknown routes, missing static files) produce a blank response.

**Fix applied:**
- Added `app.UseStatusCodePagesWithReExecute("/404")` to `Program.cs` before the static files middleware, so any 404 response triggers re-execution through the custom `/404` Razor page.

**Status:** FIXED

---

## 2026-04-04 — Articles listing page missing Schema.org/Blog JSON-LD structured data block

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.2 — JsonLdGenerator, Section 6.1 — L2-008 — Structured Data

**Description:**
The design specifies (Section 3.2, L2-008): "Listing pages emit a `Schema.org/Blog` object with a reference to the site and its articles." A prior conformance fix ("JSON-LD structured data not implemented on any page") added a `Schema.org/Blog` JSON-LD block to `Index.cshtml` (the homepage, `/`) but did not add the same block to `Pages/Articles/Index.cshtml` (the articles listing page, `/articles`). The design says "listing pages" (plural), and `/articles` is explicitly a listing page — it is the primary article discovery surface of the public site. Without a JSON-LD `<script type="application/ld+json">` block, search engine crawlers cannot extract structured metadata from the `/articles` page via the schema.org protocol, and the page fails automated SEO audit tools that check for structured data on listing pages as required by L1-003 ("perfect SEO rating across all automated audit tools").

**Fix applied:**
- Added a `@section Head` block to `Pages/Articles/Index.cshtml` containing a `<script type="application/ld+json">` with a `Schema.org/Blog` object: `@context`, `@type: "Blog"`, `name` (from `Site:SiteName` config), `description` (from `Site:SiteDescription` config), and `url` (canonical URL of `/articles` derived from `Site:SiteUrl` config). Pattern matches the JSON-LD block already on `Index.cshtml`.

**Status:** FIXED

---

## 2026-04-04 — DigitalAsset migration schema mismatch: Width/Height nullable and StoredFileName unique index missing

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 4.1 — DigitalAsset Entity; `docs/detailed-designs/10-data-persistence/README.md`, Section 3.2 — DigitalAssetConfiguration, Section 4.6 — Indexes

**Description:**
Two schema discrepancies exist between the EF Core entity configuration and the actual database migration:

1. **Width and Height nullability:** Design 04 (Section 4.1) specifies `Width int, Required` and `Height int, Required`. A prior conformance fix changed `DigitalAsset.cs` from `int?` to `int` for both fields. However, the sole migration (`20260405013757_InitialCreate.cs`) still defines these columns as `nullable: true` (`INT NULL`), and the `BlogDbContextModelSnapshot.cs` still declares them as `int?`. At runtime, EF Core will silently coerce a database `NULL` to `0` for non-nullable `int` properties, hiding any data-integrity violation. The database schema must enforce `NOT NULL` to match the entity contract and the design's Required constraint.

2. **StoredFileName unique index missing from migration:** Design 04 (Section 4.1) specifies `StoredFileName: Required, unique, max 256 chars`. A prior conformance fix added `.HasIndex(d => d.StoredFileName).IsUnique().HasDatabaseName("IX_DigitalAssets_StoredFileName")` to `DigitalAssetConfiguration.cs`. The initial migration pre-dates this fix: it creates the `StoredFileName` column as `NOT NULL NVARCHAR(256)` but never creates the unique index. The `BlogDbContextModelSnapshot.cs` likewise has no entry for this index. Without the unique constraint in the database, the storage layer has no defence against duplicate stored filenames — two concurrent uploads could receive the same GUID-based filename and overwrite each other's files on disk while the database silently accepts both records.

**Fix applied:**
- Created `src/Blog.Infrastructure/Data/Migrations/20260406000000_CorrectDigitalAssetSchema.cs` — a corrective migration that: (1) runs UPDATE statements to coerce any existing NULL values in `Width`/`Height` to `0` before the NOT NULL constraint is applied; (2) alters `Width` and `Height` from `INT NULL` to `INT NOT NULL DEFAULT 0`; (3) creates the `IX_DigitalAssets_StoredFileName` unique index on `StoredFileName`.
- Created the corresponding `20260406000000_CorrectDigitalAssetSchema.Designer.cs` with the post-migration model snapshot for EF Core's migration history tracking.
- Updated `BlogDbContextModelSnapshot.cs`: changed `Width` and `Height` properties from `int?` to `int`, and added the `IX_DigitalAssets_StoredFileName` unique index entry. The snapshot now accurately reflects the schema enforced by both migrations combined.

**Status:** FIXED

---

## 2026-04-04 — `<img>` elements inside `<picture>` blocks missing `width` and `height` attributes; browser cannot reserve layout space, causing CLS

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 7.4 — Image Pipeline (step 4)

**Description:**
The design specifies (Section 7.4, step 4): "Width and height attributes are always set to prevent CLS." CLS (Cumulative Layout Shift) is a Core Web Vitals metric; without explicit `width` and `height` attributes on `<img>` elements, the browser cannot reserve the correct space for images before they load, causing the surrounding page content to shift downward as each image arrives — a direct Core Web Vitals failure.

The `DigitalAsset` entity already stores `Width` (int) and `Height` (int) for every uploaded image (set during upload by `ImageVariantGenerator`). However, this information is never propagated to the article DTOs:

1. `ArticleDto` (used on the article detail page) has `FeaturedImageUrl` but no `FeaturedImageWidth` or `FeaturedImageHeight`.
2. `ArticleListDto` (used on the homepage and articles listing page) has `FeaturedImageUrl` but no `FeaturedImageWidth` or `FeaturedImageHeight`.

As a result, all three public Razor pages that emit `<picture>` elements for article featured images — `Pages/Articles/Slug.cshtml` (hero image), `Pages/Index.cshtml` (article card grid), and `Pages/Articles/Index.cshtml` (article card grid) — render `<img>` fallback tags without `width` or `height` attributes. The browser must wait for each image to load before it knows its intrinsic dimensions, causing layout shift as images arrive. This violates the design's CLS < 0.1 budget (Section 6) and the Core Web Vitals requirement from L2-022.

**Fix applied:**
- Added `FeaturedImageWidth` (`int?`) and `FeaturedImageHeight` (`int?`) to `ArticleDto` in `src/Blog.Api/Features/Articles/Queries/GetArticleById.cs`. Nullable so articles without a featured image or with unknown dimensions (legacy records with `Width = 0`) gracefully omit the attributes.
- Added the same two fields to `ArticleListDto` in `src/Blog.Api/Features/Articles/Queries/GetArticles.cs`.
- Updated all six DTO construction sites to populate both fields from `article.FeaturedImage?.Width`/`Height`, treating `0` as absent (sentinel value from default-initialized assets): `GetArticleByIdHandler`, `GetArticleBySlugHandler`, `GetPublishedArticleBySlugHandler`, `GetArticlesHandler`, `GetPublishedArticlesHandler`, `UpdateArticleCommandHandler`, `PublishArticleCommandHandler`. `CreateArticleCommandHandler` passes `null, null` because the newly-created article's `FeaturedImage` navigation property is not eagerly loaded within that transaction.
- Updated `Pages/Articles/Slug.cshtml`: added `width="@Model.Article.FeaturedImageWidth" height="@Model.Article.FeaturedImageHeight"` to the hero `<img>` tag when dimensions are known.
- Updated `Pages/Index.cshtml` and `Pages/Articles/Index.cshtml`: added the same conditional width/height attributes to the article card `<img>` tags.

**Status:** FIXED

---

## 2026-04-04 — CSP `style-src` and `font-src` directives block Google Fonts; fonts never load under enforced CSP

**Design reference:** `docs/detailed-designs/08-security-hardening/README.md`, Section 3.2 — SecurityHeadersMiddleware, Section 7 — Security Headers Reference

**Description:**
The design specifies `font-src 'self'` and the nonce-based `style-src 'self' 'nonce-{nonce}'` in the CSP header (Section 3.2 and Section 7). The public layout (`_Layout.cshtml`) loads the Inter typeface from Google Fonts: the stylesheet is fetched from `https://fonts.googleapis.com/css2?family=Inter:...` (via `<link rel="preload" ... onload="this.rel='stylesheet'">`), and that stylesheet in turn loads the actual font binaries from `https://fonts.gstatic.com`. Neither origin appears in `style-src` or `font-src`:

1. **`style-src 'self' 'nonce-{nonce}'`** — the Google Fonts CSS is an external stylesheet loaded without a nonce. When `onload` fires and the browser applies the stylesheet, the CSP `style-src` directive blocks it because `fonts.googleapis.com` is neither `'self'` nor the matching nonce host. In the `<noscript>` fallback the `<link rel="stylesheet">` is also blocked by the same rule.
2. **`font-src 'self'`** — even if the stylesheet were somehow applied, the actual `.woff2` font files are served from `fonts.gstatic.com`. The `font-src 'self'` directive blocks all cross-origin font requests, so the Inter typeface is never downloaded.

The result is that every visitor to the public site with a CSP-enforcing browser (all modern browsers) sees system fallback fonts instead of the designed Inter typeface. The `preconnect` and `dns-prefetch` hints for `fonts.googleapis.com` and `fonts.gstatic.com` are present in the layout but rendered useless because the fonts are blocked at the CSP level.

**Fix applied:**
- Added `https://fonts.googleapis.com` to the `style-src` directive in `SecurityHeadersMiddleware.cs` so the Google Fonts CSS stylesheet (loaded via the `<link rel="preload" onload="this.rel='stylesheet'">` tag) is permitted by the CSP.
- Added `https://fonts.gstatic.com` to the `font-src` directive so the actual `.woff2` font binary files referenced by the Google Fonts stylesheet are permitted to download.
- Removed the erroneous `img-src` placement (it was between `font-src` and `frame-ancestors`; corrected ordering to `style-src`, `font-src`, `img-src`).

**Status:** FIXED

---

## 2026-04-04 — Homepage hero `<h1>` and subtitle hardcode site name and description instead of reading from SiteConfiguration

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 4.6 — SiteConfiguration

**Description:**
The design specifies `SiteName` and `SiteDescription` as configurable values in `SiteConfiguration` (Section 4.6). Prior conformance fixes made virtually every occurrence of the site name and description in the codebase configuration-driven — the `<title>` tags, `og:site_name`, footer copyright, nav logo, JSON-LD blocks, and meta description tags all read from `Site:SiteName` and `Site:SiteDescription`. However, two visible text nodes on the homepage (`Pages/Index.cshtml`) were missed:

1. **`<h1 class="hero-title">Quinn Brown</h1>`** (line 13) — the primary heading of the homepage, the most prominent brand element on the public site, hardcodes the author/site name instead of reading `Configuration["Site:SiteName"]`.
2. **`<p class="hero-subtitle">Thoughts on software engineering, .NET architecture, and building systems that last.</p>`** (line 14) — the hero subtitle visible below the `<h1>`, which is the site description, hardcodes the same default string that `Site:SiteDescription` holds instead of reading from configuration.

Both values are already present in `appsettings.json` under `Site:SiteName` and `Site:SiteDescription`, and `IConfiguration` is already injected into `Index.cshtml`. Despite this, the most visually prominent text on the page — the hero heading and subtitle — are the last two content strings that bypass configuration. An operator who changes `Site:SiteName` to rebrand the blog would see the title tag, nav logo, footer, and meta tags update correctly, but the hero heading would still show the hardcoded original name.

**Fix applied:**
- Changed `<h1 class="hero-title">Quinn Brown</h1>` to `<h1 class="hero-title">@(Configuration["Site:SiteName"] ?? "Quinn Brown")</h1>` in `Pages/Index.cshtml`.
- Changed the hardcoded hero subtitle `<p>` to read `Configuration["Site:SiteDescription"]` with the original string as fallback.

**Status:** FIXED

---

## 2026-04-04 — Article Version property not marked as EF Core concurrency token in ArticleConfiguration

**Design reference:** `docs/detailed-designs/10-data-persistence/README.md`, Section 3.2 — Entity Configurations (ArticleConfiguration)

**Description:**
The design specifies (Section 3.2): "`Version`: required concurrency token, default 1." In EF Core, marking a property as a concurrency token causes the generated SQL `UPDATE` and `DELETE` statements to include a `WHERE Version = @originalVersion` predicate. If the row has been modified by another writer since the entity was read, EF Core detects the `0 rows affected` result and throws a `DbUpdateConcurrencyException`, enforcing optimistic concurrency at the database level.

`ArticleConfiguration` in `src/Blog.Infrastructure/Data/Configurations/ArticleConfiguration.cs` only configures `.HasDefaultValue(1)` for the `Version` property — the `.IsConcurrencyToken()` call is entirely absent. As a result, EF Core's generated `UPDATE` statements for `Article` entities include only `WHERE ArticleId = @id` with no version predicate. The application-level `If-Match` / ETag checks in the command handlers provide a first line of defence, but two concurrent requests that both pass the `If-Match` check in a race between the header check and `SaveChangesAsync` will both succeed at the database level with no detection. Any code path that bypasses the HTTP layer (seeder, `MigrationRunner`, future internal services) also has no database-level protection against lost updates.

**Fix applied:**
- Added `.IsConcurrencyToken()` to the `Version` property configuration in `src/Blog.Infrastructure/Data/Configurations/ArticleConfiguration.cs`, so EF Core includes `WHERE "Version" = @p_original_Version` in all generated `UPDATE` and `DELETE` SQL statements for `Article` entities. If a concurrent write has already incremented `Version`, EF Core will detect `0 rows affected` and throw `DbUpdateConcurrencyException`, enforcing the optimistic concurrency guarantee the design requires at the database level.

**Status:** FIXED

---

## 2026-04-04 — `article:author` Open Graph tag missing on article detail pages

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 3.1 — SeoMetaTagHelper, Section 4.2 — SeoMetadata, Section 6.1 — L2-009

**Description:**
The design's `SeoMetadata` record (Section 4.2) specifies `ArticleAuthor: string?` — "Author display name" — as a per-page SEO data field. The conformance log already shows that `article:published_time` and `article:modified_time` (also in `SeoMetadata`) were added as a gap fix. The `article:author` field is the remaining `SeoMetadata` member that was never rendered. Section 6.1 (L2-009) states: "All six OG properties and four Twitter Card properties are rendered." The Open Graph article namespace also specifies `article:author` as a supplementary article property that enables platforms like Facebook, LinkedIn, and search engines to attribute content to an author profile. `_Layout.cshtml` renders `article:published_time` and `article:modified_time` conditionally, but `article:author` is never emitted. `Slug.cshtml` sets `ViewBag.ArticlePublishedTime` and `ViewBag.ArticleModifiedTime` but never sets `ViewBag.ArticleAuthor`. As a result, social link previews and search result snippets for article pages have no author attribution signal, and the `SeoMetadata.ArticleAuthor` field defined in the design is entirely unused.

**Fix applied:**
- `Pages/Articles/Slug.cshtml`: Set `ViewBag.ArticleAuthor` to `Configuration["Site:AuthorName"]` (with `"Quinn Brown"` fallback) when the article is found, alongside the existing `ViewBag.ArticlePublishedTime` and `ViewBag.ArticleModifiedTime` assignments.
- `Pages/Shared/_Layout.cshtml`: Added `<meta property="article:author" content="@ViewBag.ArticleAuthor" />` conditionally (rendered only when `ViewBag.ArticleAuthor` is non-null), immediately after the `article:modified_time` block in the Open Graph section.

**Status:** FIXED

---

## 2026-04-04 — `ICacheInvalidator.InvalidateArticle` does not invalidate sitemap and feed cache entries; sitemap and feeds serve stale data after article state changes

**Design reference:** `docs/detailed-designs/05-seo-and-discoverability/README.md`, Section 6.3 — Caching Strategy

**Description:**
The design specifies (Section 6.3): "**Sitemap:** Cached in memory for 10 minutes. Cache is invalidated when articles are published, unpublished, or modified." and "**Feeds:** Cached in memory for 5 minutes with similar invalidation." The `ICacheInvalidator` interface exposes a single method `InvalidateArticle(string slug)`, which is called in `UpdateArticleCommandHandler` and `PublishArticleCommandHandler` after every article state change. However, the implementation of `CacheInvalidator.InvalidateArticle` only removes cache entries for the article detail page (`/articles/{slug}`), the home page (`/`, `/?page=N`), and the articles listing page (`/articles`, `/articles?page=N`). It does not remove cache entries for `/sitemap.xml`, `/feed.xml`, `/atom.xml`, or `/feed/json`.

`SeoController` uses `[ResponseCache(Duration = 600)]` on `sitemap.xml` (10 minutes) and `[ResponseCache(Duration = 300)]` on `feed.xml` and `atom.xml` (5 minutes). ASP.NET Core's `ResponseCachingMiddleware` stores cached responses in the registered `IMemoryCache` keyed by the request path. When an article is published or a title/slug is updated, the sitemap should immediately reflect the new URL and the feeds should list the newly published article — but instead both serve a cached version until the TTL expires naturally. A user who publishes an article will not see it in `/sitemap.xml` for up to 10 minutes and not in `/feed.xml` or `/atom.xml` for up to 5 minutes, even though the designed behaviour requires immediate cache eviction on article state change.

**Fix applied:**
- Updated `CacheInvalidator.InvalidateArticle` in `src/Blog.Api/Services/CacheInvalidator.cs` to also evict the response cache entries for `/sitemap.xml`, `/feed.xml`, `/atom.xml`, and `/feed/json` by calling `cache.Remove()` for each path. These removals follow the same pattern already used for the article detail and listing pages — the `ResponseCachingMiddleware` will re-render and re-cache each endpoint on the next request after invalidation.

**Status:** FIXED

---

## 2026-04-04 — IAssetStorage missing GetAsync method; AssetsController bypasses stream abstraction via GetFilePath

**Design reference:** `docs/detailed-designs/04-digital-asset-management/README.md`, Section 3.5 — AssetStorage, Section 5.2 — Serve with Optimization Flow (step 5)

**Description:**
The design specifies (Section 3.5) that `IAssetStorage` exposes four methods: `SaveAsync`, `GetAsync(string storedFileName)` — "returns a readable stream for the file", `DeleteAsync`, and `GetPublicUrl`. The implementation's `IAssetStorage` interface never added `GetAsync`. Instead it defined a filesystem-specific `GetFilePath(string storedFileName)` method and a separate `bool Exists(string storedFileName)` method. The design's serve flow (Section 5.2, step 5) reads: "Controller calls `AssetStorage.GetAsync()` to retrieve the pre-generated variant." The actual `AssetsController` bypasses this contract entirely: it calls `assetStorage.GetFilePath()` to obtain a physical path and then passes it to `PhysicalFile()`.

This defeats the abstraction boundary the design establishes. The `BlobAssetStorage` placeholder already exposes the flaw explicitly: its `GetFilePath` method throws `NotImplementedException("Azure Blob Storage does not support local file paths. Use GetUrl for cloud storage.")`. If `BlobAssetStorage` were ever registered (e.g., for staging or production), every `GET /assets/{filename}` request would crash at the `GetFilePath` call inside `AssetsController.Serve`. The design's intent was precisely to avoid this: `GetAsync` returns a `Stream` regardless of whether the backing store is a local directory, Azure Blob Storage, or S3, making the serving path backend-agnostic.

**Fix applied:**
- Added `Task<Stream?> GetAsync(string storedFileName, CancellationToken cancellationToken = default)` to `IAssetStorage` in `src/Blog.Infrastructure/Storage/IAssetStorage.cs`. Returns `null` when the file does not exist, replacing the separate `Exists` + `GetFilePath` call pattern at serving call sites.
- Implemented `GetAsync` in `LocalFileAssetStorage`: returns `null` if the file does not exist, otherwise returns a `FileStream` opened for reading with `FileOptions.SequentialScan`.
- Implemented `GetAsync` in `BlobAssetStorage`: throws `NotImplementedException` (placeholder, matching the pattern of other unimplemented blob methods).
- Updated `AssetsController.Serve`: replaced `assetStorage.Exists(variantName)` + `assetStorage.GetFilePath(variantName)` calls with `await assetStorage.GetAsync(variantName, ct)`. When a non-null stream is returned the controller sets cache/ETag/Vary headers and returns a `FileStreamResult` with the correct `Content-Type`. Replaced the final `assetStorage.Exists(fileName)` + `assetStorage.GetFilePath(fileName)` fallback with the same `GetAsync`-based pattern.

**Status:** FIXED

---

## 2026-04-04 — GetAllAsync and GetPublishedAsync load Body and BodyHtml in list queries; large columns fetched unnecessarily

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 4.2 — Entity Configuration

**Description:**
The design specifies (Section 4.2): "`Body` (Markdown source) and `BodyHtml` (pre-rendered HTML) are stored as `nvarchar(max)` / `TEXT` and **excluded from list projections**." `ArticleRepository.GetAllAsync` and `GetPublishedAsync` both use `.ToListAsync()` which materialises full `Article` entities — including the `Body` and `BodyHtml` columns, which can be many kilobytes or megabytes of Markdown source and rendered HTML per article. These fields are then silently discarded by `GetArticlesHandler` (which constructs `ArticleListDto` without `Body`/`BodyHtml`) and by `GetPublishedArticlesHandler` (which constructs `PublicArticleListDto` without them). For a page of nine articles, this needlessly transfers the full content of nine articles across the database connection on every listing request, wasting I/O bandwidth and increasing TTFB — contrary to both the design's explicit statement and the performance goal of sub-200ms TTFB at P95.

**Fix applied:**
- Replaced the `.Include(a => a.FeaturedImage).ToListAsync()` pattern in `ArticleRepository.GetAllAsync` and `GetPublishedAsync` with `.AsNoTracking().Select(a => new Article { ... Body = string.Empty, BodyHtml = string.Empty, FeaturedImage = a.FeaturedImage, ... }).ToListAsync()`. The `Select` projection causes EF Core to omit `Body` and `BodyHtml` from the generated SQL (they are not referenced in the projection), while still including the `FeaturedImage` navigation property via a LEFT JOIN (EF Core 8 translates the navigation reference inside `Select` to a join). `Body` and `BodyHtml` are set to `string.Empty` on the returned `Article` objects so callers that only use list fields receive valid (non-null) strings. The single-entity `GetByIdAsync` and `GetBySlugAsync` methods are unchanged — they continue to load all fields including `Body` and `BodyHtml` because the article detail view requires the full content.

**Status:** FIXED

---

## 2026-04-04 — Database health check timeout hardcoded instead of read from HealthChecks:DatabaseTimeoutSeconds configuration

**Design reference:** `docs/detailed-designs/09-observability/README.md`, Section 7.2 — appsettings.json Configuration, Section 3.4 — DbHealthCheck

**Description:**
The design's Section 7.2 shows `appsettings.json` containing:
```json
"HealthChecks": {
    "DatabaseTimeoutSeconds": 5
}
```
This key is the operator-visible configuration value for the database health check timeout, matching the design's Section 3.4 specification that the check "applies a timeout of 5 seconds to prevent hanging." The implementation in `Program.cs` configured the timeout by calling `reg.Timeout = TimeSpan.FromSeconds(5)` with a hardcoded integer. The `HealthChecks:DatabaseTimeoutSeconds` key was entirely absent from `appsettings.json`. As a result, an operator who needs to adjust the timeout (e.g., for a slow network link to the database in a remote environment) had no configuration surface to do so without a code change and redeployment. Any monitoring or deployment tooling that validates required configuration keys would also not find the expected `HealthChecks` section.

**Fix applied:**
- Added `"HealthChecks": { "DatabaseTimeoutSeconds": 5 }` to `src/Blog.Api/appsettings.json`, establishing the configuration key specified in the design's Section 7.2 example.
- Updated `Program.cs` to read the timeout from `builder.Configuration.GetValue<int>("HealthChecks:DatabaseTimeoutSeconds", 5)` (falling back to 5 if the key is absent for backward compatibility) and pass it to `TimeSpan.FromSeconds(dbHealthCheckTimeoutSeconds)` in the `HealthCheckServiceOptions` post-configuration, replacing the hardcoded `5`.

**Status:** FIXED

---

## 2026-04-04 — Admin articles table not replaced with card layout at SM/XS breakpoints

**Design reference:** `docs/detailed-designs/02-article-management/README.md`, Section 7.3 — Articles List — Mobile (SM/XS Breakpoints)

**Description:**
The design specifies (Section 7.3): "At 576px and below, the table is replaced with a card-based layout. Each card displays the article title, a status badge, the date, and an edit button. A hamburger menu provides access to navigation. At the XS breakpoint (375px), cards use more compact padding." The admin articles listing page (`Admin/Articles/Index.cshtml`) renders a `<table>` element at all viewport widths. The `_AdminLayout.cshtml` `max-width: 575px` media query only adjusted toolbar height, button text visibility, and the asset grid — there was no CSS to hide the table and no card HTML in the page. On a narrow mobile viewport (320–575px), the browser rendered a horizontally-scrollable table with cramped columns, degrading usability for admin users managing articles on a phone. The design's intent was a purpose-built mobile-first card layout that shows each article's essential information (title, status, date, edit action) in a scannable single-column stack.

**Fix applied:**
- Added `.article-mobile-cards` CSS class (hidden by default via `display: none; flex-direction: column`) and per-card classes (`.article-mobile-card`, `.article-mobile-card-header`, `.article-mobile-card-title`, `.article-mobile-card-slug`, `.article-mobile-card-meta`, `.article-mobile-card-date`) to `_AdminLayout.cshtml`.
- Added `max-width: 375px` rule to reduce card padding to `10px 12px` at XS, matching the design's "more compact padding at XS" requirement.
- Added `.table-container { display: none; }` and `.article-mobile-cards { display: flex; }` to the existing `max-width: 575px` media query so the table disappears and the card list appears at the SM/XS breakpoint.
- Added a `.article-mobile-cards` block in `Admin/Articles/Index.cshtml` alongside the existing table, iterating the same `Model.Articles.Items` collection. Each card renders: article title and slug (left side), edit icon button (right side), and below that a row with the status badge and formatted date — matching the four elements the design specifies.

**Status:** FIXED

---

## 2026-04-04 — `<picture>` elements missing `<source type="image/avif">` as the preferred first format source

**Design reference:** `docs/detailed-designs/07-web-performance/README.md`, Section 3.5 — ImageTagHelper; Section 7.4 — Image Pipeline

**Description:**
The design specifies (Section 3.5) that each `<picture>` element must contain two `<source>` elements in order of client preference: `<source type="image/avif">` first (highest compression, most preferred), then `<source type="image/webp">` as the fallback, then the plain `<img>` element as the final fallback. This ordering is confirmed by the output structure shown in Section 3.5:

```html
<source type="image/avif" srcset="..." sizes="...">
<source type="image/webp" srcset="..." sizes="...">
<img src="..." ...>
```

A prior conformance fix ("Article and listing images rendered as plain `<img>` instead of responsive `<picture>` with WebP srcset") added `<picture>` elements to all three public pages but only emitted the WebP source. The AVIF `<source>` element was never added to `Slug.cshtml` (hero image), `Index.cshtml` (homepage article card images), or `Articles/Index.cshtml` (listing page article card images). The `AssetsController.Serve` method already handles AVIF content negotiation via the `Accept` header and will serve pre-generated `{assetId}-{width}w.avif` variants when they exist, falling back to WebP gracefully. The `ImageVariantGenerator` notes that SixLabors.ImageSharp 3.1.x lacks a built-in AVIF encoder, so no `.avif` variant files are generated at upload time today. However, the AVIF `<source>` element must still be present in the HTML so that when AVIF generation is enabled in the future, browsers that support AVIF automatically request the correct format — the serving endpoint already handles the `image/avif` Accept header correctly. Without the AVIF `<source>`, AVIF-capable browsers (Chrome, Firefox, Safari 16+) never request the AVIF format even once it becomes available server-side, and the browser can never benefit from the higher-compression AVIF format the design requires as the preferred delivery format (L2-020, L2-029).

**Fix applied:**
- `Pages/Articles/Slug.cshtml`: Added `<source type="image/avif">` as the first source inside the hero `<picture>` element, with an AVIF srcset at breakpoints 320, 640, 960, 1280, 1920 w using the `{assetId}-{width}w.avif` naming convention. The existing `<source type="image/webp">` and `<img>` fallback are unchanged.
- `Pages/Index.cshtml`: Added `<source type="image/avif">` as the first source inside each article card `<picture>` element, with an AVIF srcset at breakpoints 320, 640, 960 w. The existing WebP source and `<img>` fallback are unchanged.
- `Pages/Articles/Index.cshtml`: Same change as `Index.cshtml` — AVIF source added first at 320/640/960 w breakpoints for article card images.
- All three pages derive the AVIF srcset entries from the same `{assetId}` extracted from `FeaturedImageUrl`, using the `{assetId}-{width}w.avif` convention matching the `ImageVariantGenerator`'s file-naming scheme. When the AVIF encoder becomes available and `.avif` variants are present on disk, browsers will transparently switch to AVIF delivery with no further code changes.

**Status:** FIXED

---

## 2026-04-04 — Password hash format does not encode the algorithm name; future algorithm migration requires a schema change

**Design reference:** `docs/detailed-designs/01-authentication/README.md`, Section 3.4 — PasswordHasher, Section 7.1 — Password Hashing

**Description:**
The design states (Section 3.4): "The hash format encodes algorithm parameters so future upgrades are backward-compatible." Section 7.1 reinforces this: "The hash format encodes the algorithm and parameters to support future migration without schema changes." The intent is that each stored hash carries enough metadata to be re-verified regardless of which algorithm the current code uses — so that if the algorithm is ever upgraded, old hashes can still be verified during a transitional period.

The `PasswordHasher.HashPassword` implementation stores hashes in the format `{salt}:{iterations}:{derivedKey}` — only three fields, none of which identifies the hashing algorithm. `VerifyPassword` always calls `Rfc2898DeriveBytes.Pbkdf2` with the hardcoded constant `HashAlgorithmName.SHA256`. If the algorithm is changed in a future release (e.g., to SHA-512 or Argon2), the `VerifyPassword` method will apply the new algorithm to all stored hashes, including those that were originally hashed with SHA-256. The derived keys will never match for pre-migration passwords, forcing every existing user to reset their password. The design's guarantee of backward-compatible future migration is broken.

The fix is to include the algorithm identifier in the stored hash format, making it a four-field value: `{algorithm}:{salt}:{iterations}:{derivedKey}`. The `VerifyPassword` method reads the algorithm field and dispatches to the correct algorithm accordingly.

**Fix applied:**
- Updated `HashPassword` in `src/Blog.Api/Services/PasswordHasher.cs` to prepend the algorithm name as the first colon-delimited field: `{algorithmName}:{base64Salt}:{iterations}:{base64DerivedKey}`. New hashes produced from this point forward include the algorithm identifier.
- Updated `VerifyPassword` to detect both formats: if four fields are present, the first field is read as the algorithm name and passed to `Rfc2898DeriveBytes.Pbkdf2`. If three fields are present (the legacy format), `SHA256` is assumed — this allows all passwords hashed before this fix to continue verifying correctly without a data migration. `Convert.FromBase64String` exceptions are caught and treated as a failed verification.

**Status:** FIXED

---

## 2026-04-04 — Search infrastructure entirely absent; IArticleRepository missing SearchAsync and GetSuggestionsAsync; no SearchHighlighter, no search/suggestions query handlers, no API endpoints

**Design reference:** `docs/detailed-designs/11-search-infrastructure/README.md`, Sections 3.2–3.8

**Description:**
Design 11 specifies a complete full-text search feature consisting of six components, all of which are absent from the codebase:

1. **`IArticleRepository` extensions** (Section 3.2): `Task<(IReadOnlyList<Article> Articles, int TotalCount)> SearchAsync(string query, int page, int pageSize, CancellationToken)` and `Task<IReadOnlyList<Article>> GetSuggestionsAsync(string query, CancellationToken)` are missing from both `IArticleRepository` and `ArticleRepository`.
2. **`ISearchHighlighter` / `SearchHighlighter`** (Section 3.5): The service that wraps matched query substrings in `<mark>` elements does not exist anywhere in the codebase. It is neither defined as an interface nor registered in DI.
3. **`SearchArticlesQuery` and handler** (Section 3.6): The MediatR query record and `SearchArticlesHandler` class do not exist. There is no file at `Features/Articles/Queries/SearchArticles.cs` or equivalent.
4. **`GetSearchSuggestionsQuery` and handler** (Section 3.7): The MediatR query record and `GetSearchSuggestionsHandler` class do not exist.
5. **`SearchResultDto` and `SearchSuggestionDto`** (Section 4.2): Neither DTO record is defined anywhere.
6. **API endpoints** (Section 3.8): `GET /api/public/articles/search` and `GET /api/public/articles/suggestions` are missing from `PublicArticlesController`. The controller only exposes `GET /` and `GET /{slug}`.

As a result, any client (the search results page, the header autocomplete, or an API consumer) that calls the designed search or suggestions endpoints receives a 404. The entire public search capability is unavailable.

**Status:** OPEN

---
