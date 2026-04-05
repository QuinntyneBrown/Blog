# Lessons from `dasblog-core`

**Source:** [poppastring/dasblog-core](https://github.com/poppastring/dasblog-core)  
**Validated:** 2026-04-04  
**Scope:** Patterns worth reusing in this codebase, not a full architecture review

This document focuses on the parts of DasBlog Core that are worth copying into our stack and the parts that are only justified by its backward-compatibility goals.

## Worth copying now

### 1. A modern host around a stable content core

DasBlog Core modernizes the application shell without forcing a rewrite of the whole content engine. The ASP.NET Core host handles configuration, DI, middleware, auth, health checks, proxies, and scheduling, while the older content runtime stays behind interfaces and managers.

Why this is worth copying:

- It keeps modernization work incremental.
- It limits the blast radius of legacy or imported code.
- It makes the hosting and operational surface easy to evolve independently from content logic.

How to apply here:

- Keep framework concerns at the edge and content behavior in application slices.
- Hide import, rendering, and publishing logic behind interfaces instead of letting controllers or pages reach into storage details.
- Preserve the current direction of Razor Pages plus vertical slices rather than building a monolith around the database model.

### 2. Small Razor building blocks instead of monolithic templates

DasBlog themes are built from Razor layouts, partials, and small Tag Helpers such as post title links, category lists, comment actions, and site metadata helpers.

Why this is worth copying:

- Themes remain composable instead of becoming copy-pasted page variants.
- UI logic stays in reusable primitives, not scattered across many views.
- Back-office and public-site markup can evolve without duplicating behavior.

How to apply here:

- Continue breaking shared UI into focused helpers and partials.
- Prefer narrow helpers such as article status badges, author chips, SEO head blocks, pagination controls, and media renderers over large reusable pages.
- Keep rendering decisions in Razor primitives and business decisions in handlers/services.

### 3. Fail-fast startup validation and self-bootstrap

DasBlog ensures expected config files exist at startup and runs a site validation step before serving requests.

Why this is worth copying:

- Broken deployments fail immediately instead of failing under traffic.
- Operators get a clear startup error instead of hidden runtime faults.
- Environment setup becomes repeatable.

How to apply here:

- Validate required configuration on startup.
- Check database connectivity, storage directories, signing keys, and any image-processing prerequisites before the app begins accepting traffic.
- Keep health checks, but also add a startup gate for conditions that should block boot entirely.

### 4. Secure-by-default public and admin surfaces

DasBlog ships with cookie auth, Identity configuration, security headers, CSP-related settings, consent handling, and spam controls wired into the default application setup.

Why this is worth copying:

- Security defaults are easier to keep than security retrofits.
- Public forms and admin pages get protection without every feature re-solving the same problem.
- Operators can tune policy through configuration rather than code edits.

How to apply here:

- Keep secure defaults in the app template, not in optional docs.
- Continue enforcing security headers, HTTPS, strong auth settings, and abuse protection as baseline behavior.
- If public forms expand beyond comments/contact, add bot resistance in the same secure-by-default style.

### 5. Move non-request work off the request path

DasBlog uses scheduled jobs and asynchronous background work for reporting and notification-style tasks rather than pushing everything through foreground requests.

Why this is worth copying:

- Publish and edit flows stay responsive.
- Operational work becomes schedulable and retryable.
- Expensive secondary tasks stop shaping the latency budget of user actions.

How to apply here:

- Offload sitemap/feed regeneration, search indexing, image variant generation, and notifications to background workers.
- Use domain events from publish/update flows so secondary work can subscribe without coupling.
- Prefer hosted services or a lightweight scheduler over ad hoc in-request processing.

### 6. Treat URL compatibility and reverse-proxy support as product features

DasBlog explicitly supports multiple permalink shapes, redirect rules, forwarded headers, and sub-path hosting.

Why this is worth copying:

- URL durability protects SEO and external links.
- Reverse proxies, subdirectory installs, and CDN setups stop being afterthoughts.
- Deployment flexibility improves without special-case patches.

How to apply here:

- Preserve canonical URLs, but support redirects from old or alternate shapes when routes evolve.
- Keep path-base, proxy header, and asset URL behavior correct in production deployments.
- Treat redirects and canonicalization as part of the publishing product, not just infrastructure trivia.

### 7. Browser tests for real user flows

DasBlog has Playwright tests that verify visible behaviors such as titles, pagination, permalink variants, feeds, comments, and route navigation.

Why this is worth copying:

- It catches regressions that unit tests miss.
- It protects public-facing behavior, not just internal code paths.
- It is especially useful when routing, rendering, and client-side enhancements interact.

How to apply here:

- Keep end-to-end coverage around login, article creation/editing, image upload, publish/unpublish, slug redirects, and SEO-critical pages.
- Use unit tests for transformation logic and browser tests for user journeys.
- Prefer a small number of durable high-signal flows over many brittle UI-detail tests.

### 8. Add integration seams without polluting the core publishing flow

DasBlog layers optional protocols such as ActivityPub and WebFinger on top of the core post model through dedicated managers and endpoints.

Why this is worth copying:

- New syndication channels can be added without rewriting authoring.
- The publish pipeline stays focused on core responsibilities.
- Experimental integrations remain easy to remove or replace.

How to apply here:

- Keep publish events and outbound integration points explicit.
- Model syndication, webhooks, feeds, and future federation as subscribers to publishing events.
- Avoid baking channel-specific logic into core article handlers.

## Not worth copying directly

These parts of DasBlog are understandable in a compatibility-focused port, but they should not guide our architecture:

- File/XML content storage as the primary persistence model.
- Singleton-heavy or static service factories where scoped application services are clearer.
- Manual background threads instead of hosted services, queues, or scheduler-managed jobs.
- `Thread.CurrentPrincipal` bridging for legacy authorization assumptions.

## Bottom line

The best parts of DasBlog Core are not its legacy internals. The parts worth copying are its migration-friendly modernization strategy, its Razor composition model, its operational discipline, its secure defaults, and its clear seams for background work and integrations.

## References

- [README](https://github.com/poppastring/dasblog-core/blob/main/README.md)
- [Program.cs](https://github.com/poppastring/dasblog-core/blob/main/source/DasBlog.Web/Program.cs)
- [DasBlogServiceCollectionExtensions.cs](https://github.com/poppastring/dasblog-core/blob/main/source/DasBlog.Web/DasBlogServiceCollectionExtensions.cs)
- [DasBlogApplicationBuilderExtensions.cs](https://github.com/poppastring/dasblog-core/blob/main/source/DasBlog.Web/DasBlogApplicationBuilderExtensions.cs)
- [DasBlogPathResolver.cs](https://github.com/poppastring/dasblog-core/blob/main/source/DasBlog.Services/FileManagement/DasBlogPathResolver.cs)
- [site.config](https://github.com/poppastring/dasblog-core/blob/main/source/DasBlog.Web/Config/site.config)
- [ActivityPubController.cs](https://github.com/poppastring/dasblog-core/blob/main/source/DasBlog.Web/Controllers/ActivityPubController.cs)
- [ActivityPubManager.cs](https://github.com/poppastring/dasblog-core/blob/main/source/DasBlog.Managers/ActivityPubManager.cs)
- [PlaywrightPageTests.cs](https://github.com/poppastring/dasblog-core/blob/main/source/DasBlog.Tests/DasBlog.Test.Integration/PlaywrightPageTests.cs)
- [Wiki: Tag Helpers and Partial Views](https://github.com/poppastring/dasblog-core/wiki/5.-Tag-Helpers-%26-Partial-Views)
- [Wiki: DasBlog architecture](https://github.com/poppastring/dasblog-core/wiki/6.-DasBlog-architecture)
- [Wiki: Migrating from DasBlog](https://github.com/poppastring/dasblog-core/wiki/7.-Migrating-from-DasBlog)
