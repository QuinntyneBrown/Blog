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
  - `POST /api/newsletter-subscriptions` — subscribe (anonymous)
  - `GET /api/newsletter-subscriptions/confirm?token={token}` — confirm (anonymous)
  - `DELETE /api/newsletter-subscriptions/{token}` — unsubscribe (anonymous, token-based)
  - `GET /api/subscribers` — paginated subscriber list (auth-guarded, for back office)
- **Security**: No authentication required for sign-up/confirm/unsubscribe. Tokens are opaque random values (not guessable). The sign-up endpoint never reveals whether an email is already subscribed (L2-054.2).

### 3.3 Command Handlers

| Handler | Command | Effect |
|---------|---------|--------|
| `CreateNewsletterHandler` | `CreateNewsletterCommand` | Persists newsletter with `status=Draft` |
| `UpdateNewsletterHandler` | `UpdateNewsletterCommand` | Updates draft; returns 409 if sent (L2-058.2) |
| `DeleteNewsletterHandler` | `DeleteNewsletterCommand` | Deletes draft; returns 409 if sent (L2-059.2) |
| `SendNewsletterHandler` | `SendNewsletterCommand` | Sets `status=Sent`, `dateSent=UtcNow`; enqueues emails (L2-060) |
| `SubscribeHandler` | `SubscribeCommand` | Upserts subscriber record; sends confirmation email (L2-054) |
| `ConfirmSubscriptionHandler` | `ConfirmSubscriptionCommand` | Validates token (48h window); marks `confirmed=true` (L2-055) |
| `UnsubscribeHandler` | `UnsubscribeCommand` | Sets `isActive=false` by unsubscribe token (L2-056) |

### 3.4 NewsletterRepository

- **Responsibility**: Data access for `Newsletter` and `NewsletterSubscriber` entities.
- **Key methods**: `GetByIdAsync`, `GetAllAsync(page, pageSize, status?)`, `GetConfirmedSubscribersAsync`, `GetSubscribersAsync(page, pageSize)`
- **Notes**: List queries project only `Subject`, `Status`, `DateSent`, `CreatedAt` — `Body`/`BodyHtml` excluded for performance (same pattern as `ArticleRepository`).

### 3.5 IEmailSender

- **Responsibility**: Abstraction over the transactional email provider. Implementations may use SMTP, SendGrid, or similar.
- **Key methods**: `SendConfirmationEmailAsync(email, confirmUrl)`, `SendNewsletterEmailAsync(email, subject, bodyHtml, unsubscribeUrl)`
- **Notes**: `SendNewsletterHandler` enqueues one Azure Service Bus message per confirmed subscriber and returns HTTP 202 immediately. A background `IHostedService` dequeues and calls `IEmailSender` per message with dead-letter retry.

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
| `Slug` | `nvarchar(512)` | Unique index; auto-generated from `Subject` at send time; null while Draft |
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
| `ConfirmationToken` | `nvarchar(128)?` | Random opaque token; null once confirmed |
| `TokenExpiresAt` | `datetime2?` | 48 hours from sign-up |
| `Confirmed` | `bit` | False until opt-in confirmed |
| `ConfirmedAt` | `datetime2?` | UTC timestamp of confirmation |
| `UnsubscribeToken` | `nvarchar(128)` | Random, generated at sign-up; permanent |
| `IsActive` | `bit` | False after unsubscribe |
| `ResubscribedAt` | `datetime2?` | UTC timestamp of most recent reactivation; null on first sign-up |
| `CreatedAt` | `datetime2` | UTC, set on insert |

---

## 5. Key Workflows

### 5.1 Subscribe (Double Opt-In)

![Subscribe Sequence](diagrams/sequence_subscribe.png)

Key points:
- If the email is already confirmed and active, the endpoint returns success without sending another email (prevents enumeration per L2-054.2).
- The confirmation link is valid for 48 hours (L2-055.2).
- Emails include `List-Unsubscribe` and `List-Unsubscribe-Post` headers with the unsubscribe token URL (L2-056.3).

### 5.2 Send Newsletter

![Send Newsletter Sequence](diagrams/sequence_send_newsletter.png)

Key points:
- Guard conditions: must be Draft status, must have ≥1 confirmed subscriber.
- Status transitions from `Draft` to `Sent` atomically with the `dateSent` timestamp.
- Each outbound email includes the subscriber's personal `unsubscribeToken` in the footer URL.

---

## 6. API Contracts

### Newsletter endpoints (all require `Authorization: Bearer <token>`)

| Method | Path | Body / Params | Success | Errors |
|--------|------|---------------|---------|--------|
| `POST` | `/api/newsletters` | `{ subject, body }` | 201 + `NewsletterDto` | 400, 401 |
| `PUT` | `/api/newsletters/{id}` | `{ subject, body }` | 200 + `NewsletterDto` | 400, 401, 404, 409 |
| `DELETE` | `/api/newsletters/{id}` | — | 204 | 401, 404, 409 |
| `POST` | `/api/newsletters/{id}/send` | — | 202 | 401, 404, 409, 422 |
| `GET` | `/api/newsletters?page&pageSize&status` | — | 200 + `PagedResponse<NewsletterListDto>` | 401 |

### Subscription endpoints (public)

| Method | Path | Body / Params | Success | Errors |
|--------|------|---------------|---------|--------|
| `POST` | `/api/newsletter-subscriptions` | `{ email }` | 200 | 400 |
| `GET` | `/api/newsletter-subscriptions/confirm` | `?token=` | 200 | 404, 422 |
| `DELETE` | `/api/newsletter-subscriptions/{token}` | — | 200 | 404 |

### Public archive endpoints (no auth)

| Method | Path | Params | Success | Errors |
|--------|------|--------|---------|--------|
| `GET` | `/api/newsletters/archive` | `?page&pageSize` | 200 + `PagedResponse<NewsletterArchiveDto>` | — |
| `GET` | `/api/newsletters/archive/{slug}` | — | 200 + `NewsletterDto` | 404 |

### Subscriber management (requires `Authorization`)

| Method | Path | Params | Success | Errors |
|--------|------|--------|---------|--------|
| `GET` | `/api/subscribers` | `?page&pageSize&status` | 200 + `PagedResponse<SubscriberDto>` | 401 |

### DTOs

```
NewsletterDto         { newsletterID, subject, slug, body, bodyHtml, status, dateSent, createdAt, updatedAt, version }
NewsletterListDto     { newsletterID, subject, slug, status, dateSent, createdAt }
NewsletterArchiveDto  { newsletterID, subject, slug, dateSent }
SubscriberDto         { subscriberID, email, confirmed, isActive, confirmedAt, resubscribedAt, createdAt }
```

---

## 7. Security Considerations

- **Token guessing**: Confirmation and unsubscribe tokens are `Guid.NewGuid().ToString("N")` — 122 bits of entropy. They are never exposed in logs.
- **Email enumeration**: The subscribe endpoint always returns 200 regardless of whether the email already exists (L2-054.2).
- **Sent newsletter protection**: Update and delete operations are rejected with 409 once `status=Sent`. This prevents accidental data mutation of historical records.
- **Rate limiting**: The `POST /api/newsletter-subscriptions` endpoint is covered by an IP-based rate limit to prevent abuse (same `write-endpoints` sliding-window policy used elsewhere).
- **HTML sanitisation**: `BodyHtml` is produced by `IMarkdownConverter` which wraps Markdig + HtmlSanitizer — XSS-safe.

---

## 8. Open Questions

1. **Email sending at scale**: ~~Calling `IEmailSender` in a tight loop holds the HTTP request open.~~ **Resolved**: Bulk sends use Azure Service Bus. `SendNewsletterHandler` enqueues one message per subscriber onto an Azure Service Bus queue and returns immediately (HTTP 202). A background `IHostedService` dequeues messages and calls `IEmailSender` per subscriber with built-in retry via Service Bus dead-letter handling.
2. **Newsletter slug for archive**: ~~Should the slug be auto-generated or a separate editable field?~~ **Resolved**: The slug is auto-generated from `Subject` at send time using `ISlugGenerator`, matching the Article pattern. If the generated slug conflicts, a numeric suffix is appended (e.g. `-2`). The slug is frozen after send and cannot be changed.
3. **Email provider**: ~~No concrete `IEmailSender` implementation specified.~~ **Resolved**: SendGrid. A `SendGridEmailSender` class implements `IEmailSender` using the `SendGrid` NuGet package. The API key is stored in `appsettings` under `SendGrid:ApiKey` and injected via `IOptions<SendGridOptions>`.
4. **Re-subscribe flow**: ~~New record or reactivate existing?~~ **Resolved**: The existing record is reactivated. `SubscribeHandler` checks for an existing row with the same email; if found and unsubscribed, it sets `Unsubscribed = false` and updates `ResubscribedAt`. No duplicate records are created.
