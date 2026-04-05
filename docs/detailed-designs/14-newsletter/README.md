# Newsletter — Detailed Design

## 1. Overview

The Newsletter feature allows the blog author to compose and send email newsletters to confirmed subscribers. Anonymous visitors can sign up via a public form, confirm their subscription through a double opt-in link, and unsubscribe at any time. Sent newsletters are also viewable in a public web archive.

### Requirements Traceability

| Requirement | Description |
|-------------|-------------|
| L1-014 | Newsletter authoring, publishing, subscriber management, and public archive |
| L2-054 | Newsletter subscription sign-up |
| L2-055 | Subscription confirmation (double opt-in) |
| L2-056 | Unsubscribe |
| L2-057 | Create newsletter |
| L2-058 | Edit newsletter |
| L2-059 | Delete newsletter |
| L2-060 | Send newsletter |
| L2-061 | List newsletters (back office) |
| L2-062 | Subscriber management (back office) |
| L2-063 | Public newsletter archive |

### Actors

- **Anonymous Visitor** — subscribes, confirms, unsubscribes, views archive
- **Blog Author** — creates, edits, sends, and deletes newsletters via back office

---

## 2. Architecture

### 2.1 C4 Context Diagram

![C4 Context](diagrams/c4_context.png)

### 2.2 C4 Container Diagram

![C4 Container](diagrams/c4_container.png)

### 2.3 C4 Component Diagram

![C4 Component](diagrams/c4_component.png)

---

## 3. Component Details

### 3.1 NewslettersController

- **Responsibility**: Exposes authenticated API endpoints for newsletter CRUD and send operations.
- **Interfaces**:
  - `POST /api/newsletters` — create draft
  - `PUT /api/newsletters/{id}` — update draft
  - `DELETE /api/newsletters/{id}` — delete draft
  - `POST /api/newsletters/{id}/send` — send newsletter
  - `GET /api/newsletters/{id}` — back-office detail by PK; used by the edit form to pre-populate fields
  - `GET /api/newsletters` — paginated list (with optional `status` filter)
- **Dependencies**: MediatR dispatcher, JWT middleware (all endpoints require `[Authorize]`)

### 3.2 SubscriptionsController

- **Responsibility**: Public endpoints for subscription lifecycle — sign-up, confirmation, unsubscribe.
- **Interfaces**:
  - `POST /api/newsletter-subscriptions` — subscribe (anonymous); returns 202 (confirmation email sent synchronously; 202 signals "accepted, check your email" rather than implying async processing)
  - `POST /api/newsletter-subscriptions/confirm` — confirm subscription via token in request body (anonymous); POST prevents link prefetchers from accidentally activating the confirmation
  - `DELETE /api/newsletter-subscriptions/{token}` — unsubscribe (anonymous, token-based); returns 204; idempotent — if the subscriber is already inactive the endpoint still returns 204 (prevents status enumeration)
  - `GET /api/subscribers` — paginated subscriber list (auth-guarded, for back office). The route lives on `SubscriptionsController` rather than a separate admin controller because the subscriber lifecycle (subscribe, confirm, unsubscribe, list) is cohesive within this controller. A future back-office controller split is straightforward — the route path itself is not coupled to the controller class name.
- **Security**: No authentication required for sign-up/confirm/unsubscribe. Tokens are opaque random values (not guessable). The sign-up endpoint never reveals whether an email is already subscribed (L2-054.2). The confirmation endpoint uses POST (not GET) to prevent HTTP proxies and link-preview crawlers from pre-fetching and unintentionally confirming subscriptions (OWASP A01 — using GET for state-changing operations is unsafe).
- **Unsubscribe token Referer leakage**: The unsubscribe token appears in the URL path (`DELETE /api/newsletter-subscriptions/{token}`). When a user visits a page containing third-party resources (analytics, fonts, images), browsers may send the full URL — including the token — in the `Referer` request header to those third-party origins. Mitigation: the Razor Page that renders the unsubscribe confirmation must set `Referrer-Policy: no-referrer` (or `strict-origin-when-cross-origin`) in its HTTP response headers so the token is not forwarded to third-party origins. Additionally, unsubscribe confirmation links sent in emails should be loaded as the sole navigation on that page rather than embedded in pages with external resource loads. The token itself remains sufficient as proof of intent since it is a 256-bit CSPRNG value; the Referer risk is about token disclosure, not forgery.

### 3.3 Command Handlers

| Handler | Command | Validator | Effect |
|---------|---------|-----------|--------|
| `CreateNewsletterHandler` | `CreateNewsletterCommand` | `CreateNewsletterCommandValidator` | Persists newsletter with `status=Draft` |
| `UpdateNewsletterHandler` | `UpdateNewsletterCommand` | `UpdateNewsletterCommandValidator` | Updates draft; returns 409 if sent (L2-058.2) or if `version` mismatches (optimistic concurrency) |
| `DeleteNewsletterHandler` | `DeleteNewsletterCommand` | — | Deletes draft; returns 409 if sent (L2-059.2) |
| `SendNewsletterHandler` | `SendNewsletterCommand` | — | Sets `status=Sent`, `dateSent=UtcNow`; enqueues emails (L2-060); calls `ICacheInvalidator` to bust the public archive list cache after status is committed |
| `SubscribeHandler` | `SubscribeCommand` | `SubscribeCommandValidator` | Upserts subscriber record; sends confirmation email synchronously via `IEmailSender` (L2-054). Sending one transactional email per sign-up is acceptable here — this is a single SendGrid call, not a bulk dispatch; the 202 response is held open only for that duration. If synchronous latency becomes a concern, the confirmation email can be enqueued to Service Bus in a later iteration. The `Email` column has a unique index; if two concurrent requests for the same email both pass the initial existence check and both attempt insert, the second will hit a unique constraint violation — the handler must catch this exception and treat it as a successful no-op (returning 202), not bubble a 500. |
| `ConfirmSubscriptionHandler` | `ConfirmSubscriptionCommand` | `ConfirmSubscriptionCommandValidator` | Validates token (48h window); returns 422 if token not found, already used, or expired; marks `confirmed=true` on success (L2-055). The expiry check must use strict less-than (`TokenExpiresAt < UtcNow`), meaning a token submitted at exactly the boundary instant is still accepted. This avoids penalising users who click a link in the final second of the window due to clock skew or network delay. `ConfirmSubscriptionCommandValidator` rules: `Token` must be non-empty (NotEmpty) and ≤ 128 chars (MaximumLength(128)) — matching the `ConfirmationToken` column width. Requests failing these rules are rejected with 400 before any DB lookup is attempted. |
| `UnsubscribeHandler` | `UnsubscribeCommand` | — | Sets `isActive=false` by unsubscribe token (L2-056) |

### 3.4 NewsletterRepository

- **Responsibility**: Data access for `Newsletter`, `NewsletterSubscriber`, and `NewsletterSendLog` entities.
- **Key methods**: `GetByIdAsync`, `GetAllAsync(page, pageSize, status?)`, `GetBySlugAsync(slug)`, `SlugExistsAsync(slug)`, `StreamConfirmedSubscriberIdsAsync()`, `GetSubscribersAsync(page, pageSize)`
- **`NewsletterSendLog` methods**: `SendLogExistsAsync(newsletterId, subscriberId)` — returns `bool`; called by `NewsletterEmailDispatchService` before sending to enforce idempotency. `AddSendLogAsync(entity)` — inserts a `NewsletterSendLog` row atomically with message completion. These methods belong on `NewsletterRepository` (same bounded context) rather than a separate repository class — the log table is an implementation detail of the send pipeline and not queried from any other component.
- **Notes**: List queries project only `Subject`, `Slug`, `Status`, `DateSent`, `CreatedAt` — `Body`/`BodyHtml` excluded for performance (same pattern as `ArticleRepository`). `StreamConfirmedSubscriberIdsAsync()` uses `IAsyncEnumerable<Guid>` to avoid loading all subscriber IDs into memory before enqueuing to Service Bus.

### 3.5 Query Handlers

| Handler | Query | Returns |
|---------|-------|---------|
| `GetNewslettersHandler` | `GetNewslettersQuery(page, pageSize, status?)` | Paginated list for back office |
| `GetNewsletterByIdHandler` | `GetNewsletterByIdQuery(newsletterId)` | Single newsletter by PK (back office); 404 if not found |
| `GetNewsletterArchiveHandler` | `GetNewsletterArchiveQuery(page, pageSize)` | Paginated list of sent newsletters for public archive, ordered `DateSent DESC` (most recent first) |
| `GetNewsletterBySlugHandler` | `GetNewsletterBySlugQuery(slug)` | Single sent newsletter by slug; 404 if not found or not Sent |

### 3.6 PublicNewslettersController

- **Responsibility**: Unauthenticated endpoints for the public newsletter archive. Separate from `NewslettersController` to avoid auth middleware applying globally.
- **Interfaces**:
  - `GET /api/newsletters/archive` — paginated list of sent newsletters
  - `GET /api/newsletters/archive/{slug}` — single sent newsletter detail
- **Routing constraint**: Both `NewslettersController` (`GET /api/newsletters/{id}`) and `PublicNewslettersController` (`GET /api/newsletters/archive`) share the `api/newsletters` URL prefix. ASP.NET Core attribute routing resolves this correctly because `archive` is a literal segment and takes precedence over the `{id}` parameter segment when both match — literal segments always rank higher than parameter segments in ASP.NET Core's route matching order. No special registration ordering or route constraint (e.g. `[id:guid]`) is required, but applying a `Guid` route constraint to `{id}` on `NewslettersController` (e.g. `[HttpGet("{id:guid}")]`) is recommended to make the disambiguation explicit and to reject malformed IDs with 404 rather than a handler-level error.

### 3.7 NewsletterEmailDispatchService

- **Responsibility**: Background `IHostedService` that continuously dequeues messages from the Azure Service Bus newsletter queue and dispatches one email per subscriber via `IEmailSender`. Runs as a hosted service registered in `Program.cs` alongside the web host. The class name `NewsletterEmailDispatchService` is the single concrete implementation of this consumer; it must implement `IHostedService` (or extend `BackgroundService`) and be registered with `services.AddHostedService<NewsletterEmailDispatchService>()`.
- **Dependencies**: `IEmailSender`, `NewsletterRepository` (for `SendLogExistsAsync` and `AddSendLogAsync`), `ServiceBusClient` (injected via `IOptions<ServiceBusOptions>`)
- **DI scope**: `NewsletterEmailDispatchService` is registered as a singleton (all `IHostedService` registrations are singleton-lifetime by default in ASP.NET Core). `NewsletterRepository` and the EF Core `DbContext` it depends on are scoped services. A singleton cannot hold a direct constructor reference to a scoped service — doing so captures the scoped instance for the lifetime of the host, causing stale `DbContext` state and potential cross-request data leakage. **The service must inject `IServiceScopeFactory` and create a new `IServiceScope` for each dequeued message**, resolving `NewsletterRepository` (and its `DbContext`) within that scope and disposing the scope when processing of the message is complete. `IEmailSender` may be registered as singleton or transient depending on its implementation and can be injected directly if its lifetime permits; otherwise it too must be resolved per-scope.
- **Non-transient `IEmailSender` failures**: Not all `SendNewsletterEmailAsync` failures are worth retrying. A `400 Bad Request` from SendGrid (e.g. the subscriber's email address is syntactically invalid or has been permanently rejected by the provider) will never succeed regardless of retry count. If Service Bus retries such a message up to `MaxDeliveryCount` times, the full retry budget is consumed on a single undeliverable address before the message is dead-lettered — delaying processing of all subsequent messages on the same queue (or blocking the session, if session-based queues are used). **The consumer must distinguish transient failures from permanent failures**: (a) wrap `SendNewsletterEmailAsync` in a try/catch; (b) if the caught exception indicates a permanent provider rejection (e.g. HTTP 400/422 from the email API, or a typed `PermanentDeliveryException` thrown by the `IEmailSender` implementation), **dead-letter the message immediately** by calling `ServiceBusReceiver.DeadLetterMessageAsync` with a reason string (e.g. `"PermanentProviderRejection"`) rather than abandoning it (which re-enqueues and consumes a retry); (c) emit `newsletter.send_failed` at `Error` level with the subscriberId and reason so operators can identify bad addresses. This prevents a single bad address from stalling the delivery pipeline. The `IEmailSender` implementation contract must document which exception types indicate permanent failures so the consumer can make this determination reliably.

### 3.8 IEmailSender

- **Responsibility**: Abstraction over the transactional email provider. Implementations may use SMTP, SendGrid, or similar.
- **Key methods**: `SendConfirmationEmailAsync(email, confirmUrl)`, `SendNewsletterEmailAsync(email, subject, bodyHtml, unsubscribeUrl)`
- **Notes**: `SendNewsletterHandler` enqueues one Azure Service Bus message per confirmed subscriber and returns HTTP 202 immediately. `NewsletterEmailDispatchService` (§3.7) dequeues and calls `IEmailSender` per message with dead-letter retry.
- **Service Bus consumer idempotency**: Azure Service Bus delivers messages at-least-once; a transient failure after `IEmailSender` succeeds but before the lock is released causes the same message to be redelivered. To prevent duplicate emails, each Service Bus message payload includes both `newsletterId` and `subscriberId`. The consumer checks a `NewsletterSendLog` table (columns: `NewsletterId`, `SubscriberId`, `SentAt datetime2`, unique composite index `UQ_NewsletterSendLog_Newsletter_Subscriber`) before sending; if a row already exists, the message is completed without re-sending. The log row is inserted atomically with message completion via `ServiceBusReceiver.CompleteMessageAsync`. The `SentAt` column enables a scheduled retention job to purge log rows older than a configurable threshold (e.g. 90 days) so the table does not grow unbounded.
- **Dead-letter queue (DLQ) monitoring**: Azure Service Bus moves messages to the DLQ after the configured `MaxDeliveryCount` (e.g. 10 attempts). Once in the DLQ a message will never be retried automatically — it is effectively lost unless an operator explicitly replays it. Therefore: (a) the `MaxDeliveryCount` on the newsletter queue must be set explicitly in infrastructure config (do not rely on the Service Bus default of 10); (b) the DLQ depth must be monitored via an Azure Monitor alert (threshold: DLQ count > 0) so operators are notified of any permanently failed deliveries; (c) `NewsletterEmailDispatchService` must emit a `newsletter.send_failed` structured log event at `Warning` level for every message that is abandoned (each delivery attempt), and at `Error` level when a message is dead-lettered (delivery count exhausted), so that per-subscriber failures are visible in APM tooling without requiring direct DLQ inspection; (d) operators must have a documented runbook for replaying DLQ messages — the `NewsletterSendLog` idempotency check ensures replayed messages do not re-send to subscribers who already received the email. A permanent `IEmailSender` failure (e.g. invalid SendGrid API key) will drain the entire newsletter send into the DLQ; this scenario must be treated as a P1 incident because affected subscribers will never receive the newsletter without a manual replay after the API key is corrected.
- **Service Bus enqueue failure**: `SendNewsletterHandler` persists `status=Sent` and the slug in a single DB transaction before beginning the enqueue loop. If the Service Bus enqueue subsequently fails (e.g. the queue is unavailable), the newsletter remains `Sent` in the DB but no messages were dispatched. To recover: a compensating back-office endpoint (or operator runbook) must be able to re-enqueue messages for a `Sent` newsletter by replaying `StreamConfirmedSubscriberIdsAsync()` and re-pushing to the queue. The consumer's idempotency check (`NewsletterSendLog`) ensures subscribers who already received the email are not emailed again during a replay. This failure mode must be surfaced as a structured log event `newsletter.enqueue_failed` at `Error` level with the `newsletterId` and the exception, so operators are alerted promptly.
- **Recommended DB indexes**: `IX_NewsletterSubscriber_ConfirmationToken` on `ConfirmationToken` (filtered index, non-NULL rows only — SQL Server syntax: `CREATE INDEX IX_NewsletterSubscriber_ConfirmationToken ON NewsletterSubscriber (ConfirmationToken) WHERE ConfirmationToken IS NOT NULL`; EF Core cannot generate filtered indexes from fluent configuration alone and this index must be added via a manual migration using `migrationBuilder.Sql(...)`) and `IX_NewsletterSubscriber_UnsubscribeToken` on `UnsubscribeToken` — both are lookup keys in hot paths. `IX_Newsletter_Status` on `Newsletter.Status` to support the `?status` filter on the back-office list endpoint efficiently.
- **`NewsletterSendLog` FK to `NewsletterSubscriber`**: The `NewsletterSendLog` table has a foreign key from `SubscriberId` → `NewsletterSubscriber.SubscriberId`. This FK must be configured with `ON DELETE SET NULL` (and `SubscriberId` in `NewsletterSendLog` must therefore be nullable). When a `NewsletterSubscriber` row is hard-deleted for GDPR erasure, SQL Server will null out the `SubscriberId` column in the corresponding `NewsletterSendLog` rows rather than blocking the delete or cascading a delete of the log rows. `ON DELETE CASCADE` is incorrect here because `NewsletterSendLog` rows are audit records of emails dispatched and must not be removed when a subscriber is erased; `ON DELETE RESTRICT` (the default) would block the erasure entirely, violating GDPR obligations.

---

## 4. Data Model

### 4.1 Class Diagram

![Class Diagram](diagrams/class_diagram.png)

### 4.2 Entity Descriptions

#### Newsletter

| Field | Type | Notes |
|-------|------|-------|
| `NewsletterId` | `Guid` | PK |
| `Subject` | `nvarchar(512)` | Required |
| `Slug` | `nvarchar(512)` | Filtered unique index on non-NULL values (SQL Server syntax: `CREATE UNIQUE INDEX IX_Newsletter_Slug ON Newsletter (Slug) WHERE Slug IS NOT NULL`; EF Core cannot generate filtered unique indexes from fluent configuration alone — this index must be added via a manual migration using `migrationBuilder.Sql(...)`); auto-generated from `Subject` at send time; NULL while Draft (SQL Server allows multiple NULLs in a unique index) |
| `Body` | `nvarchar(max)` | Raw Markdown source |
| `BodyHtml` | `nvarchar(max)` | Pre-rendered, sanitized HTML |
| `Status` | `tinyint` | Enum: `Draft=0`, `Sent=1` |
| `DateSent` | `datetime2?` | Set when status transitions to Sent |
| `CreatedAt` | `datetime2` | UTC, set on insert |
| `UpdatedAt` | `datetime2` | UTC, updated on save |
| `Version` | `int` | Optimistic concurrency; starts at `1` on first insert, incremented by `1` on each update |

#### NewsletterSubscriber

| Field | Type | Notes |
|-------|------|-------|
| `SubscriberId` | `Guid` | PK |
| `Email` | `nvarchar(256)` | Unique index |
| `ConfirmationToken` | `nvarchar(128)?` | 64-char hex string from CSPRNG (`RandomNumberGenerator.GetBytes(32)`); null once confirmed |
| `TokenExpiresAt` | `datetime2?` | 48 hours from sign-up |
| `Confirmed` | `bit` | False until opt-in confirmed |
| `ConfirmedAt` | `datetime2?` | UTC timestamp of confirmation |
| `UnsubscribeToken` | `nvarchar(128)` | 64-char hex string from CSPRNG (`RandomNumberGenerator.GetBytes(32)`); generated at sign-up; **intentionally not rotated on resubscribe** — the unsubscribe token is embedded in the footer of every previously sent newsletter email and those links must remain valid indefinitely. Rotating the token on resubscribe would silently break all unsubscribe links in archived emails the subscriber may have retained, preventing them from unsubscribing via those links. The confirmation token (used only once to activate the subscription) is always rotated on resubscribe (§8 Q4); only the unsubscribe token is kept stable. |
| `IsActive` | `bit` | False after unsubscribe |
| `ResubscribedAt` | `datetime2?` | UTC timestamp of most recent reactivation; null on first sign-up |
| `CreatedAt` | `datetime2` | UTC, set on insert |
| `UpdatedAt` | `datetime2` | UTC, updated whenever the row changes (confirm, unsubscribe, resubscribe). Provides a single "last modified" timestamp useful for change-feed auditing and as a cache-busting signal on the subscriber management UI. |

#### NewsletterSendLog

| Field | Type | Notes |
|-------|------|-------|
| `NewsletterSendLogId` | `Guid` | PK |
| `NewsletterId` | `Guid` | FK → `Newsletter.NewsletterId`; `ON DELETE CASCADE` (log rows are meaningless without the newsletter) |
| `SubscriberId` | `Guid?` | FK → `NewsletterSubscriber.SubscriberId`; nullable; `ON DELETE SET NULL` (erasure must not block; see FK note below) |
| `SentAt` | `datetime2` | UTC timestamp when the email was confirmed sent by the Service Bus consumer |

**Unique constraint**: `UQ_NewsletterSendLog_Newsletter_Subscriber` on `(NewsletterId, SubscriberId)` — enforces idempotency; the consumer checks for an existing row before sending and inserts the row atomically with message completion.

**Recommended index**: The unique constraint index on `(NewsletterId, SubscriberId)` also serves as the lookup index for the idempotency check. A separate index on `SentAt` is only needed if the retention purge job filters by `SentAt` alone rather than using a batch-delete approach; for a 90-day purge a table scan on a small log table is acceptable.

---

## 5. Key Workflows

### 5.1 Subscribe (Double Opt-In)

![Subscribe Sequence](diagrams/sequence_subscribe.png)

Key points:
- If the email is already confirmed and active, the endpoint returns success without sending another email (prevents enumeration per L2-054.2).
- The confirmation link directs the visitor to a page that submits a `POST` request containing the token; this prevents link-prefetching tools from accidentally confirming subscriptions (L2-055.1).
- The confirmation token is valid for 48 hours (L2-055.2).
- On confirmation, `ConfirmationToken` is set to `null` and `TokenExpiresAt` is cleared — tokens are single-use and cannot be replayed.
- Emails include `List-Unsubscribe` and `List-Unsubscribe-Post` headers with the unsubscribe token URL (L2-056.3).
- **Expired token cleanup**: Subscriber rows whose `Confirmed = false` and `TokenExpiresAt < UtcNow` are never automatically removed by any request handler. A scheduled background job (e.g. a nightly `IHostedService` or Hangfire job) must delete rows where `Confirmed = false AND TokenExpiresAt < UtcNow` to prevent indefinite accumulation of unconfirmed records. Until the cleanup job is implemented, operators should document this as a known operational gap. **Index note**: The filtered index `IX_NewsletterSubscriber_ConfirmationToken` (covering `ConfirmationToken IS NOT NULL`) is optimised for token-lookup hot paths (confirm and unsubscribe flows); it is **not** usable by the cleanup job query, which filters on `Confirmed` and `TokenExpiresAt`. The cleanup job must either perform a small-table scan (acceptable at typical blog subscriber volumes) or rely on a separate composite index `IX_NewsletterSubscriber_Confirmed_TokenExpiresAt` on `(Confirmed, TokenExpiresAt)` added via a manual migration. Add this index if subscriber volume grows to the point where the nightly cleanup scan causes measurable I/O pressure.

### 5.2 Send Newsletter

![Send Newsletter Sequence](diagrams/sequence_send_newsletter.png)

Key points:
- Guard conditions: must be Draft status, must have ≥1 confirmed active subscriber. Returns 422 otherwise.
- Status transitions from `Draft` to `Sent` atomically with the `dateSent` timestamp and `Slug` generation before any messages are enqueued. If `ISlugGenerator` produces an empty or blank slug (e.g. the `Subject` contains only non-ASCII characters), `SendNewsletterHandler` must fall back to the newsletter's `NewsletterId` (formatted as a lowercase hex string without hyphens) as the slug, ensuring uniqueness without blocking the send. **Slug collision retry cap**: When the generated slug conflicts with an existing newsletter, the handler appends a numeric suffix (`-2`, `-3`, etc.) and retries. This retry loop must have a hard cap of **10 attempts**. If no unique slug is found within 10 attempts, the handler must fall back to the newsletter's `NewsletterId` hex string as the slug (guaranteed unique). Without a cap, an adversary who creates many newsletters with nearly identical subjects could force an unbounded loop.
- Subscriber IDs are streamed via `IAsyncEnumerable` and enqueued to Azure Service Bus in batches of 100 to avoid memory pressure on large lists. **Zero-subscriber race**: the ≥1 subscriber guard runs before the DB transaction commits status to `Sent`. Between that check and the streaming enqueue, subscribers may unsubscribe, resulting in zero messages enqueued even though the newsletter transitions to `Sent`. This is accepted as an edge case: the newsletter is still correctly marked Sent (no emails were sent because no subscribers remained at send time), and the operator replay path can re-enqueue if needed. `SendNewsletterHandler` must log `newsletter.sent` with `recipientCount=0` at `Warning` level (rather than `Information`) when the streamed count is zero, so the anomalous outcome is visible in APM tooling without requiring a full replay.
- Each outbound email includes the subscriber's personal `unsubscribeToken` in the footer URL.

---

## 6. API Contracts

### Newsletter endpoints (all require `Authorization: Bearer <token>`)

| Method | Path | Body / Params | Success | Errors |
|--------|------|---------------|---------|--------|
| `POST` | `/api/newsletters` | `{ subject, body }` | 201 + `NewsletterDto` | 400, 401 |
| `PUT` | `/api/newsletters/{id}` | `{ subject, body, version }` | 200 + `NewsletterDto` | 400, 401, 404, 409 |
| `DELETE` | `/api/newsletters/{id}` | — | 204 | 401, 404, 409 |
| `POST` | `/api/newsletters/{id}/send` | — | 202 | 401, 404, 409, 422, 503 (Service Bus unavailable — newsletter status is rolled back to Draft if the failure occurs before the DB commit; if it occurs after DB commit the failure is logged as `newsletter.enqueue_failed` and requires operator replay) |
| `GET` | `/api/newsletters/{id}` | — | 200 + `NewsletterDto` | 401, 404 |
| `GET` | `/api/newsletters?page&pageSize&status` (default pageSize=20, max 50) | — | 200 + `PagedResponse<NewsletterListDto>` | 401 |

### Subscription endpoints (public)

| Method | Path | Body / Params | Success | Errors |
|--------|------|---------------|---------|--------|
| `POST` | `/api/newsletter-subscriptions` | `{ email }` | 202 | 400, 429 |
| `POST` | `/api/newsletter-subscriptions/confirm` | `{ token }` | 200 | 422, 429 |
| `DELETE` | `/api/newsletter-subscriptions/{token}` | — | 204 | 404 |

### Public archive endpoints (no auth)

| Method | Path | Params | Success | Errors |
|--------|------|--------|---------|--------|
| `GET` | `/api/newsletters/archive` | `?page&pageSize` | 200 + `PagedResponse<NewsletterArchiveDto>` | — |
| `GET` | `/api/newsletters/archive/{slug}` | — | 200 + `NewsletterArchiveDetailDto` | 404 |

**Caching**: Both public archive endpoints are served with `Cache-Control: public, max-age=300, stale-while-revalidate=600`. The newsletter archive is append-only (new newsletters are added but existing ones are immutable once sent), so a 5-minute TTL with a 10-minute stale window is safe. When a new newsletter is sent, `SendNewsletterHandler` must call `ICacheInvalidator` to bust the `/api/newsletters/archive` list cache so the new entry is visible within the TTL window. The detail endpoint cache (`/api/newsletters/archive/{slug}`) need not be invalidated on send — the detail page for a slug does not exist until it is sent, so there is no stale entry to evict.

**ETag / conditional GET**: The public archive endpoints do not generate ETag or Last-Modified response headers. Clients and reverse proxies therefore cannot perform conditional requests (`If-None-Match` / `If-Modified-Since`) for efficient revalidation — every revalidation fetches the full response body. This is acceptable for the current scale. If bandwidth or origin load becomes a concern, the list endpoint should add a `Last-Modified` header derived from the most-recently-sent newsletter's `DateSent`, and the detail endpoint should add a `Last-Modified` header from the newsletter's `DateSent`. ETag generation is an alternative but requires a stable hash over the response body.

### Subscriber management (requires `Authorization`)

| Method | Path | Params | Success | Errors |
|--------|------|--------|---------|--------|
| `GET` | `/api/subscribers` | `?page&pageSize&status` (status: `confirmed` \| `unconfirmed` \| `inactive`; default pageSize=20, max 50) | 200 + `PagedResponse<SubscriberDto>` | 401 |

**Subscriber count for send confirmation UI**: The `PagedResponse<SubscriberDto>` envelope includes a `totalCount` field (total rows matching the current `status` filter). The back-office "send newsletter" flow must call `GET /api/subscribers?status=confirmed&pageSize=1` to obtain the `totalCount` of confirmed active subscribers and display "You are about to send to N subscribers" before the author confirms the send. A dedicated count endpoint is not needed — the paginated list with `pageSize=1` returns the total without fetching subscriber rows.

### DTOs

```
NewsletterDto              { newsletterId, subject, slug, body, bodyHtml, status, dateSent, createdAt, updatedAt, version }
NewsletterListDto          { newsletterId, subject, slug?, status, dateSent, createdAt }  // slug is null while status=Draft
NewsletterArchiveDto       { subject, slug, dateSent }                           // newsletterId omitted — public consumers navigate by slug; exposing PK enables GUID enumeration
NewsletterArchiveDetailDto { subject, slug, bodyHtml, dateSent }                 // newsletterId omitted; body (Markdown) omitted — public consumers need only rendered HTML
SubscriberDto              { subscriberId, email, confirmed, isActive, confirmedAt, resubscribedAt, createdAt }
```

---

## 7. Security Considerations

- **Token guessing**: Confirmation and unsubscribe tokens are generated with `RandomNumberGenerator.GetBytes(32)` (256 bits from a CSPRNG), hex-encoded to a 64-character string. `Guid.NewGuid()` must NOT be used — version-4 GUIDs use a non-cryptographic pseudo-random number generator on some runtimes, making the token space predictable. Tokens are stored as-is in the database and are never exposed in logs.
- **Email enumeration**: The subscribe endpoint always returns 202 regardless of whether the email already exists (L2-054.2).
- **Sent newsletter protection**: Update and delete operations are rejected with 409 once `status=Sent`. This prevents accidental data mutation of historical records. Note: this feature uses status-based immutability (Sent newsletters are never physically deleted) rather than the hard-delete-with-guard pattern used by Events (which requires unpublish before delete) or the history-table pattern used by About. The divergence is intentional: sent newsletters are permanent records of communications dispatched to subscribers and must be preserved for audit and archive purposes; Events and About content have no such audit obligation.
- **Rate limiting**: The `POST /api/newsletter-subscriptions` and `POST /api/newsletter-subscriptions/confirm` endpoints are covered by an IP-based rate limit to prevent abuse (same `write-endpoints` sliding-window policy used elsewhere). Rate limiting the confirm endpoint prevents brute-force token enumeration.
- **Input length validation**: Command handlers enforce the DB column limits as server-side validation rules: `Subject` ≤ 512 chars, `Email` ≤ 256 chars (per RFC 5321 §4.5.3). Requests that exceed these limits are rejected with 400 before any persistence occurs.
- **Pagination bounds**: `page` must be ≥ 1 and `pageSize` must be ≥ 1 and ≤ 50. Requests outside these bounds are rejected with 400. Zero or negative values cause division-by-zero or negative offsets in pagination math and must not be forwarded to the repository.
- **Content-Type enforcement**: All `POST` and `PUT` endpoints must require `Content-Type: application/json`. Requests with a missing or mismatched `Content-Type` are rejected with 415 (Unsupported Media Type). This prevents silent null-model-binding failures when clients send `application/x-www-form-urlencoded` or other content types.
- **HTML sanitisation**: `BodyHtml` is produced by `IMarkdownConverter` which wraps Markdig + HtmlSanitizer — XSS-safe.
- **Token URL security**: Confirmation and unsubscribe links are only ever sent over HTTPS. The `confirmUrl` and `unsubscribeUrl` passed to `IEmailSender` are always constructed with `https://` scheme. Tokens are invalidated immediately on first use.
- **GDPR — right to erasure**: Unsubscribing sets `IsActive = false` and clears `ConfirmationToken` but retains the subscriber row and email address. If a subscriber invokes the right to erasure, `SubscribeHandler`'s upsert logic must detect a hard-deleted row cannot be re-used. The system must support a hard-delete path: a back-office action that physically removes the `NewsletterSubscriber` row, nulls any `NewsletterSendLog` references, and removes the email address. This path is not exposed publicly — it is an admin operation. Until implemented, operators must document the erasure procedure in a Data Protection Impact Assessment.
- **Observability**: Key lifecycle operations must emit structured log events at `Information` level: `subscriber.subscribed` (email hash only, never plaintext), `subscriber.confirmed`, `subscriber.unsubscribed`, `newsletter.sent` (newsletterId, recipientCount). `newsletter.send_failed` (newsletterId, subscriberId, error) must be emitted at `Warning` level (each individual delivery failure in the Service Bus consumer) — this distinguishes it from the `Information`-level success events and ensures delivery failures surface in monitoring without being lost in routine operational noise. `newsletter.enqueue_failed` (newsletterId, exception) must be emitted at `Error` level when the Service Bus enqueue loop fails after the newsletter status has been committed as Sent — this is an actionable alert requiring operator replay. Tokens and plaintext emails must never appear in log output.

---

## 8. Open Questions

1. **Email sending at scale**: ~~Calling `IEmailSender` in a tight loop holds the HTTP request open.~~ **Resolved**: Bulk sends use Azure Service Bus. `SendNewsletterHandler` enqueues one message per subscriber onto an Azure Service Bus queue and returns immediately (HTTP 202). A background `IHostedService` dequeues messages and calls `IEmailSender` per subscriber with built-in retry via Service Bus dead-letter handling.
2. **Newsletter slug for archive**: ~~Should the slug be auto-generated or a separate editable field?~~ **Resolved**: The slug is auto-generated from `Subject` at send time using `ISlugGenerator`, matching the Article pattern. If the generated slug conflicts, a numeric suffix is appended (e.g. `-2`). The slug is frozen after send and cannot be changed.
3. **Email provider**: ~~No concrete `IEmailSender` implementation specified.~~ **Resolved**: SendGrid. A `SendGridEmailSender` class implements `IEmailSender` using the `SendGrid` NuGet package. The API key is stored in `appsettings` under `SendGrid:ApiKey` and injected via `IOptions<SendGridOptions>`.
4. **Re-subscribe flow**: ~~New record or reactivate existing?~~ **Resolved**: The existing record is reactivated. `SubscribeHandler` checks for an existing row with the same email; if found with `IsActive = false`, it sets `IsActive = true`, generates a **new** CSPRNG confirmation token (overwriting any stale `ConfirmationToken`), resets `TokenExpiresAt` to 48 hours from now, and updates `ResubscribedAt`. A new confirmation email is sent with the new token. No duplicate records are created. The old token must not be reused — it may have already expired or been observed by an attacker who intercepted the original confirmation email.
