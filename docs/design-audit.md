# Detailed Design Audit

**Date:** 2026-04-05
**Scope:** All 10 detailed design documents, 20 architecture decision records, L1/L2 requirements, README, and supporting documentation.

---

## Table of Contents

1. [Cross-Document Inconsistencies](#1-cross-document-inconsistencies)
2. [Per-Feature Findings](#2-per-feature-findings)
3. [Security Findings](#3-security-findings)
4. [Performance Findings](#4-performance-findings)
5. [Best Practice Findings](#5-best-practice-findings)
6. [Missing or Incomplete Items](#6-missing-or-incomplete-items)
7. [Recommendations](#7-recommendations)

---

## 1. Cross-Document Inconsistencies

### 1.1 Article endpoint path — `/api/posts` vs `/api/articles`

| Source | Path used |
|--------|-----------|
| L2.md (canonical) | `/api/posts` |
| README.md | `/api/posts` |
| 02-article-management | `/api/articles` |
| 06-restful-api | `/api/articles` |
| 09-observability | `/api/posts` |
| ADR-0004 (RESTful API) | `/api/articles` |

L2.md is the requirements source-of-truth and uses `/api/posts`. The detailed designs for features 02 and 06 and the corresponding ADR use `/api/articles`. These should be reconciled so that every document uses one path consistently.

### 1.2 Authentication endpoint path

| Source | Path used |
|--------|-----------|
| README.md | `POST /api/users/authenticate` |
| 01-authentication | `POST /api/auth/login`, `POST /api/auth/refresh` |
| 08-security-hardening | `/api/auth/*` |
| ADR security/0001 | `POST /api/auth/login`, `POST /api/auth/refresh` |

README.md is the only document that uses `/api/users/authenticate`. All detailed designs and ADRs agree on `/api/auth/login` and `/api/auth/refresh`.

### 1.3 Digital asset upload endpoint

| Source | Path used |
|--------|-----------|
| README.md | `POST /api/digital-assets/upload` |
| 04-digital-asset-management | `POST /api/digital-assets` |
| 06-restful-api | `POST /api/digital-assets` |
| ADR backend/0009 | `POST /api/digital-assets` |

README.md appends `/upload` to the path. The detailed designs follow standard REST conventions where the HTTP method (`POST`) already implies creation.

### 1.4 Abstract field maximum length

| Source | Max length |
|--------|-----------|
| 02-article-management entity model | 512 characters |
| 10-data-persistence configuration | 1024 characters |
| 10-data-persistence SQL schema | `VARCHAR(1024)` |

Feature 02 and Feature 10 disagree. The data persistence design is the authoritative source for storage constraints and specifies 1024.

### 1.5 Authentication rate limit

| Source | Limit |
|--------|-------|
| L2.md L2-027 (canonical) | 10 requests per minute per IP |
| 08-security-hardening | 10 requests per minute per client IP |
| ADR security/0005 | 10 per minute per IP |
| 01-authentication | 5 attempts per email per 15 minutes |

Feature 01 (Authentication) defines a different limit with a different scope (per-email rather than per-IP). All other documents, including the L2 requirement, agree on 10 per minute per IP.

### 1.6 XL breakpoint reference value

| Source | Value |
|--------|-------|
| L1.md / L2.md (canonical) | >= 1200 px |
| ADR frontend/0002 | >= 1200 px |
| README.md (design context) | 1440 px |
| 03-public-article-display (design reference) | 1440 px |

The 1440 px value is the design canvas width, not the CSS breakpoint. The implementation breakpoint is >= 1200 px. The README phrasing "XS 375px, SM 576px, MD 768px, LG 992px, XL 1440px" conflates the design canvas with the breakpoint and should clarify that 1440 px is the design reference while the media query triggers at 1200 px.

---

## 2. Per-Feature Findings

### 2.1 Feature 01 — Authentication & Authorization

| # | Type | Finding |
|---|------|---------|
| F01-1 | Open question | Refresh token strategy (rotation vs. short-lived access-only) is unresolved. This affects token revocation capability and the security posture of the entire admin surface. |
| F01-2 | Open question | Password hashing algorithm not finalized. Design lists PBKDF2, bcrypt, and Argon2 as options. ADR security/0002 accepts PBKDF2, but Feature 01 still lists it as open. |
| F01-3 | Open question | Account lockout after N failed attempts vs. rate limiting only is unresolved. Rate limiting alone does not prevent slow, distributed brute-force attacks across many IPs. |
| F01-4 | Gap | No token revocation mechanism. If a JWT is compromised, it remains valid until expiration. A server-side deny-list or refresh-token revocation would mitigate this. |
| F01-5 | Inconsistency | Rate limit in this document (5 per 15 min per email) conflicts with L2-027 and Feature 08 (10 per min per IP). See §1.5. |

### 2.2 Feature 02 — Article Management

| # | Type | Finding |
|---|------|---------|
| F02-1 | Open question | Article body format (HTML, Markdown, or both) is unresolved. The storage format, rendering pipeline, and editor experience all depend on this decision. |
| F02-2 | Design concern | Slug regeneration on title change can break public URLs for published articles. The design does not restrict slug changes to drafts or implement redirect-from-old-slug behavior. |
| F02-3 | Gap | No concurrency control. There is no ETag, row-version, or optimistic locking on the Article entity. Simultaneous edits by multiple admins risk silent data loss. |
| F02-4 | Gap | No soft delete or audit trail. Permanent deletion with no recovery option is risky for production content management. |
| F02-5 | Inconsistency | Abstract max length (512) differs from Feature 10 (1024). See §1.4. |
| F02-6 | Scope gap | UI designs show category badges on article cards, but the data model has no category or tag field and no L2 requirement defines the feature. Either remove from UI designs or add to requirements. |

### 2.3 Feature 03 — Public Article Display

| # | Type | Finding |
|---|------|---------|
| F03-1 | Open question | Whether the public web app calls the API over HTTP or invokes services in-process is unresolved. This directly impacts TTFB, deployment topology, and the ability to scale the API and web tiers independently. |
| F03-2 | Gap | Default page size is not specified. The listing grid is 3 columns on XL, suggesting the page size should be a multiple of 3 (e.g., 9 or 12) for a visually balanced layout. The API default of 20 would leave a partial final row. |
| F03-3 | Gap | No output caching TTL or strategy is documented for SSR pages in this feature. Feature 07 specifies `max-age=60, stale-while-revalidate=600`, but Feature 03 does not reference it. |
| F03-4 | Observation | SkeletonCard loading states are mentioned, but SSR delivers complete HTML on first response. Skeleton states are only useful if client-side pagination or infinite scroll is added later. |

### 2.4 Feature 04 — Digital Asset Management

| # | Type | Finding |
|---|------|---------|
| F04-1 | Open question | Image processing library (SixLabors.ImageSharp vs. SkiaSharp vs. System.Drawing) is not selected. ADR backend/0009 flags this as an open issue. |
| F04-2 | Gap | No maximum pixel dimension limit on uploads. A 10 MB JPEG could be 20000 × 20000 px, consuming excessive CPU and memory during processing. A pixel-count or dimension cap would prevent this. |
| F04-3 | Open question | Image variant generation strategy (eager at upload time vs. lazy on first request) is unresolved. Eager increases storage but guarantees fast first-request serving. Lazy reduces storage but introduces cold-start latency. |
| F04-4 | Gap | No virus or malware scanning for uploaded files. Content-type validation by magic bytes prevents extension spoofing but does not detect embedded malicious payloads. |
| F04-5 | Gap | Asset deletion behavior is not fully specified. If an article references a deleted asset via `FeaturedImageId`, the FK is nullified (ON DELETE SET NULL), but inline body `<img>` references in article HTML are not cleaned up, producing broken images. |

### 2.5 Feature 05 — SEO & Discoverability

| # | Type | Finding |
|---|------|---------|
| F05-1 | Gap | Sitemap protocol requires a sitemap index when the number of URLs exceeds 50,000. The design does not handle this case. For a personal blog this is unlikely but the design should acknowledge the limit. |
| F05-2 | Open question | Feed entry content depth (full HTML body vs. abstract only) is not specified. Full body improves the feed reader experience but increases bandwidth. |
| F05-3 | Observation | `llms.txt` is described as an "emerging convention" with no formal standard. The implementation should be easy to update as the convention matures. |

### 2.6 Feature 06 — RESTful API

| # | Type | Finding |
|---|------|---------|
| F06-1 | Gap | No API versioning scheme. While deferral is acceptable at launch, the design should document the planned versioning approach (URL path, header, or query parameter) so consumers can prepare. |
| F06-2 | Gap | No ETag support on GET endpoints. Conditional requests with `If-None-Match` would reduce bandwidth for unchanged resources. Feature 07 mentions ETag on article detail HTML pages but not on API responses. |
| F06-3 | Gap | Response envelope wrapping for non-JSON responses (file downloads, health check, sitemap) is not addressed. These endpoints should be excluded from the `ApiResponse<T>` envelope. |
| F06-4 | Inconsistency | Endpoint catalog uses `/api/articles` but L2.md specifies `/api/posts`. See §1.1. |

### 2.7 Feature 07 — Web Performance

| # | Type | Finding |
|---|------|---------|
| F07-1 | Gap | No CDN strategy. Geographic distribution directly affects P95 TTFB for users far from the origin. The design should state whether a CDN is deferred, planned, or unnecessary. |
| F07-2 | Gap | No database query time budget. TTFB < 200 ms is the end-to-end target, but no allocation is made for database round-trip latency, leaving no margin analysis. |
| F07-3 | Open question | Critical CSS extraction method (manual per-template vs. automated via headless browser at build time) is unresolved. For a small number of templates, manual extraction is simpler and avoids a build-time dependency. |
| F07-4 | Gap | No RUM analytics integration. `web-vitals` library is mentioned (< 1 KB) but no collection endpoint or reporting destination is defined. Without RUM, production Core Web Vitals compliance cannot be verified. |

### 2.8 Feature 08 — Security Hardening

| # | Type | Finding |
|---|------|---------|
| F08-1 | Concern | CSP uses `style-src 'self' 'unsafe-inline'`. This weakens the Content Security Policy for styles. ADR security/0004 acknowledges this as a pragmatic concession for critical CSS inlining and flags nonce-based injection as a future improvement. |
| F08-2 | Concern | Rate limit counters are in-memory. They do not survive server restarts and do not synchronize across multiple instances. ADR security/0005 documents Redis as the migration path for horizontal scaling. |
| F08-3 | Gap | No CSP violation reporting. Adding `report-uri` or `report-to` directives would provide visibility into policy violations in production. |
| F08-4 | Gap | No malware scanning on uploaded assets. See F04-4. |

### 2.9 Feature 09 — Observability

| # | Type | Finding |
|---|------|---------|
| F09-1 | Open question | Log aggregation platform (Seq, ELK, Datadog, Azure Monitor) is not selected. JSON output is platform-agnostic, but the operational team needs a chosen platform to build dashboards and alerts. |
| F09-2 | Gap | No alerting strategy. Health checks expose status but no alerts are triggered on unhealthy status, error rate spikes, or elevated latency. |
| F09-3 | Gap | No OpenTelemetry integration. Serilog structured logging provides request-level observability, but distributed tracing via OpenTelemetry would enable richer diagnostics and is increasingly expected. |

### 2.10 Feature 10 — Data Persistence

| # | Type | Finding |
|---|------|---------|
| F10-1 | Open question | Primary database (PostgreSQL vs. SQL Server) is not selected. The SQL examples use PostgreSQL syntax (`gen_random_uuid()`, `TIMESTAMPTZ`), but the choice is listed as open. EF Core provider, connection pooling, and deployment strategy all depend on this. |
| F10-2 | Gap | No backup or disaster recovery strategy. Database backup frequency, retention, RTO/RPO targets, and restore procedures are not documented. |
| F10-3 | Gap | No full-text search strategy. Searching article content requires either a database full-text index or an external search service. The design does not address this. |
| F10-4 | Observation | SQL schema includes `Create INDEX` (capital C on `Create`) for `IX_Articles_CreatedAt`. This is a typo; it should be `CREATE INDEX` for consistency. |

---

## 3. Security Findings

### 3.1 Strengths

- OWASP Top 10 is systematically addressed in Feature 08 with a mapping table.
- Defense-in-depth via ordered middleware pipeline (HTTPS → headers → rate limit → CORS → auth → validation → sanitization).
- Passwords hashed with PBKDF2-SHA256 at >= 100,000 iterations with per-user salt and self-describing format for future algorithm migration.
- All database queries parameterized via EF Core, eliminating SQL injection.
- HTML sanitized at write time with allow-list via Ganss.Xss HtmlSanitizer.
- JWT signing key >= 256 bits; authorization header never logged.
- Sensitive fields (passwords, tokens, PII) explicitly excluded from logs via LogSanitizer.
- HSTS with preload, X-Frame-Options DENY, Referrer-Policy, Permissions-Policy all configured.

### 3.2 Concerns

| # | Finding | Severity |
|---|---------|----------|
| S-1 | **No JWT revocation.** Compromised tokens remain valid until expiry (default 60 minutes). A server-side deny-list checked on each request would limit exposure. | High |
| S-2 | **CSP allows `'unsafe-inline'` for styles.** This permits injection of arbitrary CSS, which can exfiltrate data via CSS selectors in some scenarios. Nonce-based or hash-based style injection is recommended. | Medium |
| S-3 | **Rate limit state is in-memory only.** A server restart resets all counters. Multiple instances cannot share state. Distributed rate limiting (e.g., Redis) is needed before horizontal scaling. | Medium |
| S-4 | **No uploaded-file malware scanning.** Magic-byte validation prevents extension spoofing but does not detect malicious payloads embedded in valid image files. | Medium |
| S-5 | **No account lockout.** Rate limiting slows brute-force attacks but does not lock accounts after repeated failures. Slow distributed attacks across many IPs could bypass IP-based rate limits. | Medium |
| S-6 | **No user roles or permissions.** All authenticated users have full admin access. A compromised account has unrestricted write access to all content. | Medium |
| S-7 | **PBKDF2 is not memory-hard.** It is more vulnerable to GPU-accelerated attacks than Argon2. ADR security/0002 documents this and provides a migration path. | Low |

---

## 4. Performance Findings

### 4.1 Strengths

- Explicit, measurable targets: TTFB < 200 ms at P95, Lighthouse 100 (mobile), LCP < 2.5 s, INP < 200 ms, CLS < 0.1, JS <= 50 KB gzipped.
- Server-side rendering eliminates client-side hydration and JS framework overhead.
- Layered caching: immutable static assets (1 year), HTML pages (60 s + stale-while-revalidate 600 s), in-memory response cache.
- Brotli compression (level 4 dynamic, level 11 pre-compressed static) with Gzip fallback.
- Critical CSS inlined; remaining CSS loaded asynchronously.
- Responsive images via `<picture>` with AVIF/WebP sources and lazy loading below the fold.
- Width and height attributes on all images to prevent CLS.
- ETag-based conditional responses on article detail pages.

### 4.2 Concerns

| # | Finding | Severity |
|---|---------|----------|
| P-1 | **No CDN.** All requests hit the origin server. P95 TTFB < 200 ms depends on user proximity to the server. A CDN would reduce latency for geographically distributed readers. | Medium |
| P-2 | **No database query budget.** The 200 ms TTFB target includes DB round-trip, rendering, and compression. Without an explicit query-time allocation (e.g., < 50 ms), there is no early-warning mechanism for slow queries. | Medium |
| P-3 | **In-memory cache does not scale horizontally.** If a second instance is added, each builds its own cache independently, increasing database load and cache miss rates. | Medium |
| P-4 | **Image variant generation strategy is undefined.** Lazy on-demand generation adds latency to the first request for each size/format combination. Eager pre-generation at upload time is more predictable but uses more storage. | Low |
| P-5 | **Listing page size vs. grid alignment.** Default page size of 20 does not divide evenly into a 3-column grid. A page size of 9 or 12 would prevent partial final rows. | Low |

---

## 5. Best Practice Findings

| # | Area | Finding |
|---|------|---------|
| BP-1 | **Concurrency control** | No optimistic concurrency (ETag or row-version) on Article updates. Multiple simultaneous editors can silently overwrite each other's changes. Add a `RowVersion`/`ConcurrencyToken` column and return 409 Conflict on version mismatch. |
| BP-2 | **Soft delete** | Articles are permanently deleted with no recovery option. A `DeletedAt` timestamp with a configurable retention window and a scheduled hard-delete job would protect against accidental deletion. |
| BP-3 | **Audit trail** | No record of who changed an article or when. An audit log table or change-tracking mechanism would support accountability and incident investigation. |
| BP-4 | **Slug immutability after publish** | Changing a published article's slug breaks public URLs, social shares, and search engine indexes. The design should either prevent slug changes on published articles or automatically create a 301 redirect from the old slug. |
| BP-5 | **OpenAPI specification** | No OpenAPI/Swagger document is generated. Auto-generating an OpenAPI spec from controllers would provide always-current API documentation and enable client SDK generation. |
| BP-6 | **API versioning plan** | No versioning scheme is documented. Even if v1 is implicit at launch, stating the planned approach (URL segment, header, or query parameter) avoids a breaking-change surprise for consumers later. |
| BP-7 | **Health check granularity** | The health endpoint returns a single `healthy`/`unhealthy` status. Separating liveness (process up) from readiness (dependencies available) is a Kubernetes/load-balancer best practice and supports zero-downtime deployments. |
| BP-8 | **Event-driven cache invalidation** | Sitemap, feeds, and response cache are invalidated by direct calls when an article is published or updated. A lightweight domain-event pattern (e.g., `ArticlePublished` event) would decouple producers from consumers and simplify adding future subscribers. |
| BP-9 | **Testing strategy** | CONTRIBUTING.md describes an ATDD workflow and the folder structure defines test projects, but no testing strategy document exists covering the test pyramid, integration test patterns, performance/load testing, security testing, or accessibility testing automation. |
| BP-10 | **Backup and disaster recovery** | No backup frequency, retention policy, RTO/RPO targets, or restore procedures are documented anywhere in the design. |

---

## 6. Missing or Incomplete Items

### 6.1 Unresolved open questions across designs

The detailed designs contain open questions that must be resolved before implementation can begin:

| Feature | Open Question | Impact |
|---------|--------------|--------|
| 01 – Authentication | Refresh token strategy (rotation vs. short-lived only) | Token revocation capability |
| 02 – Article Management | Article body format (HTML, Markdown, or both) | Content storage, editor UX, rendering pipeline |
| 03 – Public Display | Public web app calls API over HTTP vs. in-process service | TTFB, deployment topology |
| 04 – Digital Assets | Image processing library (ImageSharp vs. SkiaSharp) | Platform support, licensing, CPU cost |
| 04 – Digital Assets | Eager vs. lazy image variant generation | Storage vs. first-request latency |
| 07 – Performance | Critical CSS extraction method (manual vs. automated) | Build pipeline complexity |
| 09 – Observability | Log aggregation platform | Operational dashboards and alerting |
| 10 – Data Persistence | Primary database (PostgreSQL vs. SQL Server) | EF Core provider, migrations, deployment |

### 6.2 Missing design documents

| Topic | Gap |
|-------|-----|
| Deployment & infrastructure | No topology, scaling strategy, or operational runbook. |
| Monitoring & alerting | No alert rules, thresholds, or escalation procedures. |
| Backup & disaster recovery | No backup schedule, retention, RTO/RPO, or restore procedures. |
| Testing strategy | No test pyramid, load-testing plan, or accessibility-testing automation. |
| Search | No article search capability for public readers. |
| Content governance | No roles, permissions, approval workflows, or article scheduling. |

---

## 7. Recommendations

### Resolve before implementation

1. **Reconcile endpoint paths.** Choose either `/api/posts` or `/api/articles` and update all documents and the README to match.
2. **Reconcile auth endpoint.** Update README.md from `/api/users/authenticate` to `/api/auth/login`.
3. **Reconcile digital-asset upload path.** Update README.md from `/api/digital-assets/upload` to `POST /api/digital-assets`.
4. **Fix Abstract max length.** Update Feature 02 from 512 to 1024 to match Feature 10 and the SQL schema.
5. **Fix auth rate limit in Feature 01.** Update from "5 per 15 min per email" to "10 per min per IP" to match L2-027, Feature 08, and the ADR.
6. **Clarify XL breakpoint in README.** Note that 1440 px is the design canvas and the CSS breakpoint is >= 1200 px.
7. **Decide article body format.** Document whether the body is stored as HTML, Markdown, or both. Specify when conversion occurs.
8. **Select the image processing library.** Evaluate SixLabors.ImageSharp (managed, portable) vs. SkiaSharp (native, faster) and document the decision in a new ADR.
9. **Select the primary database.** PostgreSQL or SQL Server affects the EF Core provider, migration scripts, and deployment. Document the decision in the existing open ADR.

### Resolve before production

10. **Add optimistic concurrency** to the Article entity (row-version column, 409 on conflict).
11. **Prevent slug changes on published articles** or implement automatic 301 redirects from old slugs.
12. **Add JWT revocation** via a server-side deny-list or refresh-token rotation.
13. **Add CSP violation reporting** (`report-to` directive) to monitor policy violations.
14. **Replace CSP `'unsafe-inline'` for styles** with nonce-based injection.
15. **Document backup and disaster recovery** procedures with RTO/RPO targets.
16. **Document a deployment topology** and operational runbook.
17. **Document a monitoring and alerting strategy** including alert thresholds and escalation.
18. **Document a testing strategy** covering the test pyramid, load testing, security testing, and accessibility testing.
19. **Introduce liveness and readiness health checks** (`/health/live` and `/health/ready`).
20. **Add OpenAPI/Swagger generation** to provide self-documenting API endpoints.
