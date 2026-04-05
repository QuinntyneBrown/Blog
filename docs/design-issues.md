# Design Issues Audit

Audit date: 2026-04-04

Scope: `docs/detailed-designs` and supporting ADRs that define the current platform architecture.

Goal: close the major consistency, performance, security, and best-practice gaps so the design set is implementation-ready.

## Completed Issues

| ID | Area | Issue | Resolution | Status |
|----|------|-------|------------|--------|
| DI-001 | Authentication | `User.Salt` was documented separately even though the PBKDF2 hash format already stores salt and parameters. | Removed the separate salt field from the auth design and aligned it with the persistence model and password-hashing ADR. | Complete |
| DI-002 | Authentication | Refresh flow required the current valid access token, which broke reload recovery and encouraged unsafe session behavior. | Removed silent refresh from v1 and standardized on short-lived access tokens with explicit re-authentication on expiry. | Complete |
| DI-003 | Authentication | Password hashing guidance still presented PBKDF2, bcrypt, and Argon2 as unresolved options. | Standardized the design set on PBKDF2-SHA256 per the accepted ADR. | Complete |
| DI-004 | Authentication | Login responses in feature docs drifted from the API envelope contract. | Updated auth response examples to use the standard `{ data, timestamp }` envelope. | Complete |
| DI-005 | Rate limiting | Auth rate limits conflicted between per-email and per-IP policies. | Standardized on layered auth throttling: per-IP and per-normalized-email, plus per-user write limits. | Complete |
| DI-006 | Security | CORS behavior still implied credentialed cross-origin requests by default. | Set `AllowCredentials` default to `false` and updated behavior text to treat credentialed CORS as an explicit exception. | Complete |
| DI-007 | Security | Validation error examples did not consistently use RFC 7807 problem type URIs. | Updated the security design to use RFC 7807-compliant validation problem details. | Complete |
| DI-008 | Security | OWASP mapping still claimed bcrypt despite the accepted PBKDF2 decision. | Corrected the cryptographic-failures mapping to PBKDF2. | Complete |
| DI-009 | Security | CSP guidance lacked `object-src 'none'` and still referenced CSS-in-JS/front-end framework assumptions. | Tightened the CSP and updated the explanation for Razor Pages and server-rendered critical CSS. | Complete |
| DI-010 | Security | Antiforgery guidance did not reflect Razor Pages form handling. | Clarified that Razor Pages forms use antiforgery tokens, while bearer-token API clients do not rely on ambient browser credentials. | Complete |
| DI-011 | Public site architecture | Public article display still described the public site as calling the API server over HTTP. | Standardized on Razor Pages using shared published-content services in-process within the ASP.NET Core application boundary. | Complete |
| DI-012 | Public article UX | SSR pages still referenced skeleton loading states that are unnecessary for initial render. | Removed skeleton-loading guidance from the public article design. | Complete |
| DI-013 | Public article data model | Article cards referenced category tags that are not part of the requirements or data model. | Removed category/tag language from the public article display design. | Complete |
| DI-014 | Public article querying | Published-state filtering still used `Status == Published` while the data model uses `Published : bool`. | Standardized on `Published == true` across public-content workflows. | Complete |
| DI-015 | SEO | The SEO design still described the public web app as “Razor Pages / MVC”. | Standardized the public platform wording to Razor Pages. | Complete |
| DI-016 | SEO | `robots.txt` built the sitemap URL from the incoming host, conflicting with canonical URL security guidance. | Standardized all absolute public URLs on configured `SiteUrl` rather than the request host. | Complete |
| DI-017 | REST API | Article contracts in the API design included unsupported `tags` fields and a string `status` model that drifted from the article design. | Updated the API contracts to use the article model actually defined by the system: `published`, `datePublished`, `abstract`, `featuredImageId`, and `readingTimeMinutes`. | Complete |
| DI-018 | REST API | Publish/unpublish behavior drifted between separate endpoints and a boolean publish payload. | Standardized publish state changes on `PATCH /api/articles/{id}/publish` with a `{ "published": true|false }` body. | Complete |
| DI-019 | REST API | Article editing lacked an explicit optimistic concurrency strategy. | Added version-based optimistic concurrency with `ETag`/`If-Match` semantics for update, publish, and delete operations. | Complete |
| DI-020 | REST API | Digital asset contracts conflicted on whether metadata or binary content was returned from `GET /api/digital-assets/{id}`. | Standardized `GET /api/digital-assets/{id}` as authenticated metadata retrieval and `/assets/{filename}` as the public binary route. | Complete |
| DI-021 | REST API | Health-check responses exposed dependency details on the public endpoint. | Standardized `/health` as a minimal public status endpoint and documented detailed readiness checks separately. | Complete |
| DI-022 | API best practices | Pagination link generation was described in a way that could encourage raw host-header trust. | Clarified that navigation links are generated via framework URL generation using configured application URLs rather than blindly echoing raw headers. | Complete |
| DI-023 | Web performance | Cache keying and compression strategy were inconsistent and risked incorrect cache behavior. | Standardized on caching canonical uncompressed HTML and compressing on the way out. | Complete |
| DI-024 | Web performance | Middleware order contradicted static-file short-circuiting and cache ownership. | Clarified middleware order so static files are handled before HTML response caching, while compression remains outermost for outgoing responses. | Complete |
| DI-025 | Web performance | Strong ETags computed from rendered HTML before compression were not compatible with the documented cache/compression flow. | Switched to weak, version-based validators suitable for compressed and uncompressed representations. | Complete |
| DI-026 | Web performance | Resource-hint examples referenced third-party font hosts that conflicted with the CSP. | Updated performance guidance to use first-party font and image preloads only. | Complete |
| DI-027 | Web performance | Lighthouse 100 was treated as a brittle hard CI gate. | Kept 100 as the product target but changed the automated regression floor to 95 to avoid flaky enforcement while still driving the design toward 100. | Complete |
| DI-028 | Observability | Correlation IDs were accepted verbatim from clients without validation. | Added validation and regeneration rules for invalid or oversized inbound correlation IDs. | Complete |
| DI-029 | Observability | Request log examples still referenced `/api/posts`. | Standardized examples and business events on `/api/articles`. | Complete |
| DI-030 | Observability | Logging guidance relied mainly on deny-list scrubbing and did not forbid body/auth-header logging by default. | Clarified that request and response bodies plus authorization headers are excluded by default, with the deny-list acting as defense in depth. | Complete |
| DI-031 | Data persistence | Article constraints drifted from the article-management design (`Abstract` max length and reading-time minimum/default). | Standardized `Abstract` to 512 characters and `ReadingTimeMinutes` to minimum/default 1. | Complete |
| DI-032 | Data persistence | Digital asset length constraints drifted between the persistence and asset-management designs. | Standardized file-name and content-type limits across the docs. | Complete |
| DI-033 | Data persistence | The persistence design lacked an explicit concurrency token for article editing. | Added an article `Version` concurrency token used by optimistic concurrency in the API and article-management designs. | Complete |
| DI-034 | Data persistence | Default-admin seeding via `HasData()` was unsafe and operationally misleading. | Replaced it with deployment-time admin bootstrap/provisioning guidance and removed privileged-account seeding from migrations. | Complete |
| DI-035 | Digital assets | File-size limits were documented, but decompression/pixel-count abuse was still an open gap. | Added maximum dimension/pixel-count validation guidance in the asset pipeline. | Complete |

## Residual Notes

- Rendered diagram PNG artifacts were not regenerated in this audit pass. Source documentation was updated; image regeneration can be handled separately when PlantUML/Graphviz is available.
