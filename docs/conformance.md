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
