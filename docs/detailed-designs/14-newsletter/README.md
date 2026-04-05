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
  - `GET /api/newsletters` — paginated list (with optional `status` filter)
- **Dependencies**: MediatR dispatcher, JWT middleware (all endpoints require `[Authorize]`)

### 3.2 SubscriptionsController

- **Responsibility**: Public endpoints for subscription lifecycle — sign-up, confirmation, unsubscribe.
- **Interfaces**:
  - `POST /api/newsletter-subscriptions` — subscribe (anonymous); returns 202 (confirmation email enqueued)
  - `POST /api/newsletter-subscriptions/confirm` — confirm subscription via token in request body (anonymous); POST prevents link prefetchers from accidentally activating the confirmation
  - `DELETE /api/newsletter-subscriptions/{token}` — unsubscribe (anonymous, token-based); returns 204; idempotent — if the subscriber is already inactive the endpoint still returns 204 (prevents status enumeration)
  - `GET /api/subscribers` — paginated subscriber list (auth-guarded, for back office)
- **Security**: No authentication required for sign-up/confirm/unsubscribe. Tokens are opaque random values (not guessable). The sign-up endpoint never reveals whether an email is already subscribed (L2-054.2). The confirmation endpoint uses POST (not GET) to prevent HTTP proxies and link-preview crawlers from pre-fetching and unintentionally confirming subscriptions (OWASP A01 — using GET for state-changing operations is unsafe).

### 3.3 Command Handlers

| Handler | Command | Effect |
|---------|---------|--------|
| `CreateNewsletterHandler` | `CreateNewsletterCommand` | Persists newsletter with `status=Draft` |
| `UpdateNewsletterHandler` | `UpdateNewsletterCommand` | Updates draft; returns 409 if sent (L2-058.2) or if `version` mismatches (optimistic concurrency) |
| `DeleteNewsletterHandler` | `DeleteNewsletterCommand` | Deletes draft; returns 409 if sent (L2-059.2) |
| `SendNewsletterHandler` | `SendNewsletterCommand` | Sets `status=Sent`, `dateSent=UtcNow`; enqueues emails (L2-060) |
| `SubscribeHandler` | `SubscribeCommand` | Upserts subscriber record; sends confirmation email (L2-054). The `Email` column has a unique index; if two concurrent requests for the same email both pass the initial existence check and both attempt insert, the second will hit a unique constraint violation — the handler must catch this exception and treat it as a successful no-op (returning 202), not bubble a 500. |
| `ConfirmSubscriptionHandler` | `ConfirmSubscriptionCommand` | Validates token (48h window); returns 422 if token not found, already used, or expired; marks `confirmed=true` on success (L2-055) |
| `UnsubscribeHandler` | `UnsubscribeCommand` | Sets `isActive=false` by unsubscribe token (L2-056) |

### 3.4 NewsletterRepository

- **Responsibility**: Data access for `Newsletter` and `NewsletterSubscriber` entities.
- **Key methods**: `GetByIdAsync`, `GetAllAsync(page, pageSize, status?)`, `GetBySlugAsync(slug)`, `SlugExistsAsync(slug)`, `StreamConfirmedSubscriberIdsAsync()`, `GetSubscribersAsync(page, pageSize)`
- **Notes**: List queries project only `Subject`, `Slug`, `Status`, `DateSent`, `CreatedAt` — `Body`/`BodyHtml` excluded for performance (same pattern as `ArticleRepository`). `StreamConfirmedSubscriberIdsAsync()` uses `IAsyncEnumerable<Guid>` to avoid loading all subscriber IDs into memory before enqueuing to Service Bus.

### 3.5 Query Handlers

| Handler | Query | Returns |
|---------|-------|---------|
| `GetNewslettersHandler` | `GetNewslettersQuery(page, pageSize, status?)` | Paginated list for back office |
| `GetNewsletterArchiveHandler` | `GetNewsletterArchiveQuery(page, pageSize)` | Paginated list of sent newsletters for public archive |
| `GetNewsletterBySlugHandler` | `GetNewsletterBySlugQuery(slug)` | Single sent newsletter by slug; 404 if not found or not Sent |

### 3.6 PublicNewslettersController

- **Responsibility**: Unauthenticated endpoints for the public newsletter archive. Separate from `NewslettersController` to avoid auth middleware applying globally.
- **Interfaces**:
  - `GET /api/newsletters/archive` — paginated list of sent newsletters
  - `GET /api/newsletters/archive/{slug}` — single sent newsletter detail

### 3.7 IEmailSender

- **Responsibility**: Abstraction over the transactional email provider. Implementations may use SMTP, SendGrid, or similar.
- **Key methods**: `SendConfirmationEmailAsync(email, confirmUrl)`, `SendNewsletterEmailAsync(email, subject, bodyHtml, unsubscribeUrl)`
- **Notes**: `SendNewsletterHandler` enqueues one Azure Service Bus message per confirmed subscriber and returns HTTP 202 immediately. A background `IHostedService` dequeues and calls `IEmailSender` per message with dead-letter retry.
- **Service Bus consumer idempotency**: Azure Service Bus delivers messages at-least-once; a transient failure after `IEmailSender` succeeds but before the lock is released causes the same message to be redelivered. To prevent duplicate emails, each Service Bus message payload includes both `newsletterID` and `subscriberID`. The consumer checks a `NewsletterSendLog` table (columns: `NewsletterID`, `SubscriberID`, `SentAt datetime2`, unique composite index `UQ_NewsletterSendLog_Newsletter_Subscriber`) before sending; if a row already exists, the message is completed without re-sending. The log row is inserted atomically with message completion via `ServiceBusReceiver.CompleteMessageAsync`. The `SentAt` column enables a scheduled retention job to purge log rows older than a configurable threshold (e.g. 90 days) so the table does not grow unbounded.
- **Service Bus enqueue failure**: `SendNewsletterHandler` persists `status=Sent` and the slug in a single DB transaction before beginning the enqueue loop. If the Service Bus enqueue subsequently fails (e.g. the queue is unavailable), the newsletter remains `Sent` in the DB but no messages were dispatched. To recover: a compensating back-office endpoint (or operator runbook) must be able to re-enqueue messages for a `Sent` newsletter by replaying `StreamConfirmedSubscriberIdsAsync()` and re-pushing to the queue. The consumer's idempotency check (`NewsletterSendLog`) ensures subscribers who already received the email are not emailed again during a replay. This failure mode must be surfaced as a structured log event `newsletter.enqueue_failed` at `Error` level with the `newsletterID` and the exception, so operators are alerted promptly.
- **Recommended DB indexes**: `IX_NewsletterSubscriber_ConfirmationToken` on `ConfirmationToken` (filtered index, non-NULL rows only) and `IX_NewsletterSubscriber_UnsubscribeToken` on `UnsubscribeToken` — both are lookup keys in hot paths. `IX_Newsletter_Status` on `Newsletter.Status` to support the `?status` filter on the back-office list endpoint efficiently.

---

## 4. Data Model

### 4.1 Class Diagram

![Class Diagram](diagrams/class_diagram.png)

### 4.2 Entity Descriptions

#### Newsletter

| Field | Type | Notes |
|-------|------|-------|
| `NewsletterID` | `Guid` | PK |
| `Subject` | `nvarchar(512)` | Required |
| `Slug` | `nvarchar(512)` | Filtered unique index on non-NULL values; auto-generated from `Subject` at send time; NULL while Draft (SQL Server allows multiple NULLs in a unique index) |
| `Body` | `nvarchar(max)` | Raw Markdown source |
| `BodyHtml` | `nvarchar(max)` | Pre-rendered, sanitized HTML |
| `Status` | `tinyint` | Enum: `Draft=0`, `Sent=1` |
| `DateSent` | `datetime2?` | Set when status transitions to Sent |
| `CreatedAt` | `datetime2` | UTC, set on insert |
| `UpdatedAt` | `datetime2` | UTC, updated on save |
| `Version` | `int` | Optimistic concurrency |

#### NewsletterSubscriber

| Field | Type | Notes |
|-------|------|-------|
| `SubscriberID` | `Guid` | PK |
| `Email` | `nvarchar(256)` | Unique index |
| `ConfirmationToken` | `nvarchar(128)?` | 64-char hex string from CSPRNG (`RandomNumberGenerator.GetBytes(32)`); null once confirmed |
| `TokenExpiresAt` | `datetime2?` | 48 hours from sign-up |
| `Confirmed` | `bit` | False until opt-in confirmed |
| `ConfirmedAt` | `datetime2?` | UTC timestamp of confirmation |
| `UnsubscribeToken` | `nvarchar(128)` | 64-char hex string from CSPRNG (`RandomNumberGenerator.GetBytes(32)`); generated at sign-up; permanent |
| `IsActive` | `bit` | False after unsubscribe |
| `ResubscribedAt` | `datetime2?` | UTC timestamp of most recent reactivation; null on first sign-up |
| `CreatedAt` | `datetime2` | UTC, set on insert |

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
- **Expired token cleanup**: Subscriber rows whose `Confirmed = false` and `TokenExpiresAt < UtcNow` are never automatically removed by any request handler. A scheduled background job (e.g. a nightly `IHostedService` or Hangfire job) must delete rows where `Confirmed = false AND TokenExpiresAt < UtcNow` to prevent indefinite accumulation of unconfirmed records. Until the cleanup job is implemented, operators should document this as a known operational gap.

### 5.2 Send Newsletter

![Send Newsletter Sequence](diagrams/sequence_send_newsletter.png)

Key points:
- Guard conditions: must be Draft status, must have ≥1 confirmed active subscriber. Returns 422 otherwise.
- Status transitions from `Draft` to `Sent` atomically with the `dateSent` timestamp and `Slug` generation before any messages are enqueued. If `ISlugGenerator` produces an empty or blank slug (e.g. the `Subject` contains only non-ASCII characters), `SendNewsletterHandler` must fall back to the newsletter's `NewsletterID` (formatted as a lowercase hex string without hyphens) as the slug, ensuring uniqueness without blocking the send.
- Subscriber IDs are streamed via `IAsyncEnumerable` and enqueued to Azure Service Bus in batches of 100 to avoid memory pressure on large lists.
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

### Subscriber management (requires `Authorization`)

| Method | Path | Params | Success | Errors |
|--------|------|--------|---------|--------|
| `GET` | `/api/subscribers` | `?page&pageSize&status` (status: `confirmed` \| `unconfirmed` \| `inactive`; default pageSize=20, max 50) | 200 + `PagedResponse<SubscriberDto>` | 401 |

### DTOs

```
NewsletterDto              { newsletterID, subject, slug, body, bodyHtml, status, dateSent, createdAt, updatedAt, version }
NewsletterListDto          { newsletterID, subject, slug?, status, dateSent, createdAt }  // slug is null while status=Draft
NewsletterArchiveDto       { subject, slug, dateSent }                           // newsletterID omitted — public consumers navigate by slug; exposing PK enables GUID enumeration
NewsletterArchiveDetailDto { subject, slug, bodyHtml, dateSent }                 // newsletterID omitted; body (Markdown) omitted — public consumers need only rendered HTML
SubscriberDto              { subscriberID, email, confirmed, isActive, confirmedAt, resubscribedAt, createdAt }
```

---

## 7. Security Considerations

- **Token guessing**: Confirmation and unsubscribe tokens are generated with `RandomNumberGenerator.GetBytes(32)` (256 bits from a CSPRNG), hex-encoded to a 64-character string. `Guid.NewGuid()` must NOT be used — version-4 GUIDs use a non-cryptographic pseudo-random number generator on some runtimes, making the token space predictable. Tokens are stored as-is in the database and are never exposed in logs.
- **Email enumeration**: The subscribe endpoint always returns 202 regardless of whether the email already exists (L2-054.2).
- **Sent newsletter protection**: Update and delete operations are rejected with 409 once `status=Sent`. This prevents accidental data mutation of historical records.
- **Rate limiting**: The `POST /api/newsletter-subscriptions` and `POST /api/newsletter-subscriptions/confirm` endpoints are covered by an IP-based rate limit to prevent abuse (same `write-endpoints` sliding-window policy used elsewhere). Rate limiting the confirm endpoint prevents brute-force token enumeration.
- **Input length validation**: Command handlers enforce the DB column limits as server-side validation rules: `Subject` ≤ 512 chars, `Email` ≤ 256 chars (per RFC 5321 §4.5.3). Requests that exceed these limits are rejected with 400 before any persistence occurs.
- **Pagination bounds**: `page` must be ≥ 1 and `pageSize` must be ≥ 1 and ≤ 50. Requests outside these bounds are rejected with 400. Zero or negative values cause division-by-zero or negative offsets in pagination math and must not be forwarded to the repository.
- **Content-Type enforcement**: All `POST` and `PUT` endpoints must require `Content-Type: application/json`. Requests with a missing or mismatched `Content-Type` are rejected with 415 (Unsupported Media Type). This prevents silent null-model-binding failures when clients send `application/x-www-form-urlencoded` or other content types.
- **HTML sanitisation**: `BodyHtml` is produced by `IMarkdownConverter` which wraps Markdig + HtmlSanitizer — XSS-safe.
- **Token URL security**: Confirmation and unsubscribe links are only ever sent over HTTPS. The `confirmUrl` and `unsubscribeUrl` passed to `IEmailSender` are always constructed with `https://` scheme. Tokens are invalidated immediately on first use.
- **GDPR — right to erasure**: Unsubscribing sets `IsActive = false` and clears `ConfirmationToken` but retains the subscriber row and email address. If a subscriber invokes the right to erasure, `SubscribeHandler`'s upsert logic must detect a hard-deleted row cannot be re-used. The system must support a hard-delete path: a back-office action that physically removes the `NewsletterSubscriber` row, nulls any `NewsletterSendLog` references, and removes the email address. This path is not exposed publicly — it is an admin operation. Until implemented, operators must document the erasure procedure in a Data Protection Impact Assessment.
- **Observability**: Key lifecycle operations must emit structured log events at `Information` level: `subscriber.subscribed` (email hash only, never plaintext), `subscriber.confirmed`, `subscriber.unsubscribed`, `newsletter.sent` (newsletterID, recipientCount), `newsletter.send_failed` (newsletterID, subscriberID, error). `newsletter.enqueue_failed` (newsletterID, exception) must be emitted at `Error` level when the Service Bus enqueue loop fails after the newsletter status has been committed as Sent — this is an actionable alert requiring operator replay. Tokens and plaintext emails must never appear in log output.

---

## 8. Open Questions

1. **Email sending at scale**: ~~Calling `IEmailSender` in a tight loop holds the HTTP request open.~~ **Resolved**: Bulk sends use Azure Service Bus. `SendNewsletterHandler` enqueues one message per subscriber onto an Azure Service Bus queue and returns immediately (HTTP 202). A background `IHostedService` dequeues messages and calls `IEmailSender` per subscriber with built-in retry via Service Bus dead-letter handling.
2. **Newsletter slug for archive**: ~~Should the slug be auto-generated or a separate editable field?~~ **Resolved**: The slug is auto-generated from `Subject` at send time using `ISlugGenerator`, matching the Article pattern. If the generated slug conflicts, a numeric suffix is appended (e.g. `-2`). The slug is frozen after send and cannot be changed.
3. **Email provider**: ~~No concrete `IEmailSender` implementation specified.~~ **Resolved**: SendGrid. A `SendGridEmailSender` class implements `IEmailSender` using the `SendGrid` NuGet package. The API key is stored in `appsettings` under `SendGrid:ApiKey` and injected via `IOptions<SendGridOptions>`.
4. **Re-subscribe flow**: ~~New record or reactivate existing?~~ **Resolved**: The existing record is reactivated. `SubscribeHandler` checks for an existing row with the same email; if found with `IsActive = false`, it sets `IsActive = true`, clears `ConfirmationToken`, resets `TokenExpiresAt`, and updates `ResubscribedAt`. A new confirmation email is sent. No duplicate records are created.
