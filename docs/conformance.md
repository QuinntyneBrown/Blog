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
The design specifies `IDigitalAssetRepository` should expose `Task<IReadOnlyList<DigitalAsset>> GetByCreatedByAsync(Guid userId)` — a method that returns only the assets belonging to a specific creator. The implementation instead declares `Task<List<DigitalAsset>> GetAllAsync(CancellationToken cancellationToken = default)`, which fetches every digital asset in the database with no creator filter. `DigitalAssetRepository` implements this with a plain `ToListAsync()` with no `Where` clause, and `GetDigitalAssetsHandler` calls `GetAllAsync` directly. As a result, every call to list digital assets performs a full table scan and returns all assets regardless of who created them, violating the design's per-creator scoping contract and the interface contract documented in the spec.

**Status:** OPEN

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
