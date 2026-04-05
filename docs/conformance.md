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

**Status:** OPEN

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
