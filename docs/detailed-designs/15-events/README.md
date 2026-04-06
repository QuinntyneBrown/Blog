# Events — Detailed Design

## 1. Overview

The Events feature allows the blog author to manage a list of speaking engagements through the back office. Published events are displayed to anonymous visitors on the public site, split into upcoming and past sections, with a detail page for each event.

### Requirements Traceability

| Requirement | Description |
|-------------|-------------|
| L1-015 | Events: creation, editing, publish/unpublish, deletion, and public display |
| L2-064 | Create event |
| L2-065 | Edit event |
| L2-066 | Delete event |
| L2-067 | List events in back office |
| L2-068 | Publish and unpublish event |
| L2-069 | Public events display (`/events`) |
| L2-070 | Public event detail (`/events/{slug}`) |

### Actors

- **Anonymous Visitor** — browses upcoming and past published events, views event detail
- **Blog Author** — creates, edits, publishes/unpublishes, and deletes events via back office

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

### 3.1 EventsController

- **Responsibility**: Exposes API endpoints for event CRUD and publish/unpublish. All write operations are auth-guarded.
- **Interfaces**:
  - `POST /api/events` — create event
  - `PUT /api/events/{id}` — update event
  - `DELETE /api/events/{id}` — delete event
  - `POST /api/events/{id}/publish` — publish
  - `POST /api/events/{id}/unpublish` — unpublish
  - `GET /api/events` — paginated list for back office
  - `GET /api/events/{id}` — back-office detail by PK; used by the edit form to pre-populate fields (auth-guarded)
  - `GET /api/events/published` — public: returns paginated upcoming + past published events (no auth)
  - `GET /api/events/by-slug/{slug}` — public event detail by slug (no auth); uses explicit prefix to avoid routing conflict with `/{id}`

### 3.2 Command Handlers

| Handler | Command | Validator | Effect |
|---------|---------|-----------|--------|
| `CreateEventHandler` | `CreateEventCommand` | `CreateEventCommandValidator` — rules: `Title` NotEmpty, MaximumLength(256); `Description` NotEmpty, MaximumLength(4000); `StartDate` NotEmpty; `TimeZoneId` NotEmpty, MaximumLength(64), Must(be a valid IANA time zone identifier — validated by attempting `TimeZoneInfo.FindSystemTimeZoneById`); `Location` NotEmpty, MaximumLength(512); `EndDate` must be ≥ `StartDate` when provided (Must predicate); `ExternalUrl` must be a well-formed absolute HTTPS URL (`https://` only) when provided (Must predicate, rejects `http://`, relative URLs, and empty strings) | Generates slug, persists with `published=false`; sets `CreatedAt = UpdatedAt = UtcNow` and `Version = 1` on insert. Computes `StartDateUtc` and `EndDateUtc` from `StartDate`/`EndDate` + `TimeZoneId` using `TimeZoneInfo.ConvertTimeToUtc` before persisting. `UpdatedAt` must be explicitly set on insert (not left to a DB default) so that the value is always populated and consistent with the EF Core entity model — EF Core does not automatically populate `UpdatedAt` on insert unless a value converter or `SaveChanges` interceptor is configured. |
| `UpdateEventHandler` | `UpdateEventCommand` | `UpdateEventCommandValidator` — rules: same field rules as `CreateEventCommandValidator` plus `Version` GreaterThan(0) | Updates all mutable fields. The handler checks `FirstPublishedAt`: if null (event has never been published), the slug is regenerated from the new title via `ISlugGenerator` and checked for uniqueness by calling `SlugExistsAsync(newSlug, excludeEventId: command.EventId)` — the current event's ID **must** be passed as `excludeEventId` so the event is excluded from its own slug conflict check; without the exclusion, an event whose title has not changed would always appear to conflict with itself and every update would return 409; returns 409 only if the new slug matches a **different** event. If `FirstPublishedAt` is not null (event was published at some point), **the slug is frozen and not regenerated** — title changes do not affect the slug. This ensures published event URLs remain stable for SEO, bookmarks, and shared links. The rest of the update proceeds unchanged: sets `UpdatedAt = UtcNow` and increments `Version` by 1 (`Version = storedVersion + 1`) before saving (both must be explicit — EF Core does not auto-set these without an interceptor); `CreatedAt` must never be modified on the update path — it is the event's creation timestamp and must remain stable; calls `ICacheInvalidator` if the event is currently `Published = true` |
| `DeleteEventHandler` | `DeleteEventCommand` | — | Removes event; returns 409 if `Published = true` (must unpublish first) |
| `PublishEventHandler` | `PublishEventCommand` | — | Receives `PublishEventCommand(eventId, version)`. Loads the event; returns 404 if not found. **Optimistic concurrency check**: if `command.Version != storedEvent.Version`, returns 409 Conflict — the client must re-fetch the current state before retrying. On version match: if already published (no-op), returns the current `EventDto` without modifying `UpdatedAt` or `Version`. If not yet published: sets `Published = true`, sets `FirstPublishedAt = UtcNow` if null (first publish only), updates `UpdatedAt = UtcNow`, increments `Version` by 1; calls `ICacheInvalidator` to bust the public events cache. The response `EventDto` carries the new `version` value so the back-office client can use it in subsequent requests without re-fetching. |
| `UnpublishEventHandler` | `UnpublishEventCommand` | — | Receives `UnpublishEventCommand(eventId, version)`. Loads the event; returns 404 if not found. **Optimistic concurrency check**: if `command.Version != storedEvent.Version`, returns 409 Conflict — the client must re-fetch the current state before retrying. On version match: if already unpublished (no-op), returns the current `EventDto` without modifying `UpdatedAt` or `Version`. If currently published: sets `Published = false`, updates `UpdatedAt = UtcNow`, increments `Version` by 1; calls `ICacheInvalidator` to bust the public events cache. The response `EventDto` carries the new `version` value so the back-office client can use it in subsequent requests without re-fetching. |

### 3.3 Query Handlers

| Handler | Query | Returns |
|---------|-------|---------|
| `GetEventsHandler` | `GetEventsQuery(page, pageSize)` | Paginated list, all statuses, descending by `StartDate` (most distant future events appear first; most recent past events at the end of the list — this order reflects a forward-planning back-office UX where upcoming engagements are the primary concern; if a "recently created/modified" view is preferred in future, sort by `CreatedAt DESC` instead) |
| `GetPublishedEventsHandler` | `GetPublishedEventsQuery(upcomingPage, pastPage, pageSize)` | `{ upcoming: PagedResponse<PublicEventDto>, past: PagedResponse<PublicEventDto> }` split by `StartDate` vs `UtcNow`; upcoming section ordered `StartDate ASC` (next event first), past section ordered `StartDate DESC` (most recent past event first). When both sections are empty (no published events), the handler returns 200 with empty `PagedResponse` arrays (`items: [], totalCount: 0`) — not 204. |
| `GetEventBySlugHandler` | `GetEventBySlugQuery(slug)` | Single published event; 404 if not found or not published |
| `GetEventByIdHandler` | `GetEventByIdQuery(eventId)` | Admin detail by ID |

Both public query handlers must compute and set `ETag` and `Last-Modified` response headers, and evaluate incoming `If-None-Match` / `If-Modified-Since` request headers to return 304 when the content has not changed (see §7 HTTP caching). The conditional check should be performed **after** loading the data but **before** serialising the response body, so that 304 responses avoid serialisation overhead.

### 3.4 EventRepository

- **Responsibility**: Data access for `Event` entities.
- **Key methods**:
  - `GetByIdAsync(eventId)` — by PK
  - `GetBySlugAsync(slug)` — by unique slug, **regardless of `Published` state** (returns any event, published or unpublished). `GetEventBySlugHandler` applies the `Published == true` check after the repository call and returns 404 for unpublished events. Implementers must not add a published filter inside this method — doing so would make unpublished events' slugs invisible to the handler's "not found" branch, but the important distinction is that `SlugExistsAsync` (not `GetBySlugAsync`) is used for slug-conflict checking on `UpdateEventHandler`, so a published-only `GetBySlugAsync` would not cause missed conflicts. The no-filter semantic is documented here to prevent confusion during implementation.
  - `GetAllAsync(page, pageSize)` — paginated, all statuses, ordered by `StartDate DESC` (puts far-future events at top; see `GetEventsHandler` note for rationale)
  - `GetUpcomingAsync(page, pageSize)` — published, `StartDateUtc >= UtcNow`, ordered `StartDateUtc ASC`
  - `GetPastAsync(page, pageSize)` — published, `StartDateUtc < UtcNow`, ordered `StartDateUtc DESC`
  - `SlugExistsAsync(slug, excludeEventId?)` — uniqueness check
  - `GetTotalUpcomingCountAsync()` — total published upcoming count for pagination metadata
  - `GetTotalPastCountAsync()` — total published past count for pagination metadata

### 3.5 ISlugGenerator

- Reused from existing infrastructure (`SlugGenerator`). Converts `Title` to a URL-safe slug. Called on create and on update **only if the event has never been published** (`FirstPublishedAt` is null). Once an event has been published, the slug is frozen and `ISlugGenerator` is not called on subsequent updates.
- On update (when `FirstPublishedAt` is null), the slug is regenerated from the new title. If the new slug conflicts with a **different** event, the handler returns 409 (L2-065.2).

---

## 4. Data Model

### 4.1 Class Diagram

![Class Diagram](diagrams/class_diagram.png)

### 4.2 Entity Description

#### Event

| Field | Type | Notes |
|-------|------|-------|
| `EventId` | `Guid` | PK |
| `Title` | `nvarchar(256)` | Required |
| `Slug` | `nvarchar(256)` | Unique index; generated from `Title` |
| `FirstPublishedAt` | `datetime2?` | UTC; set on first publish (`PublishEventHandler`); null while the event has never been published; never cleared on unpublish — retains the timestamp of the first-ever publish to indicate the slug is frozen. Once non-null, `UpdateEventHandler` skips slug regeneration. |
| `Description` | `nvarchar(max)` | Required; plain text (not Markdown). No `DescriptionHtml` field exists — the value is stored and rendered as-is without Markdown processing. Application layer enforces ≤ 4000 chars |
| `StartDate` | `datetime2` | Required; **venue-local wall-clock time** as entered by the author (not UTC). Represents the date and time of the event in the venue's time zone. |
| `EndDate` | `datetime2?` | Optional; venue-local wall-clock time. Must be ≥ `StartDate` when provided. |
| `TimeZoneId` | `nvarchar(64)` | Required; IANA time zone identifier (e.g. `America/New_York`, `Europe/London`, `Asia/Tokyo`). Validated against `TimeZoneInfo.FindSystemTimeZoneById` (on Windows, .NET maps IANA to Windows zone IDs via ICU). Used to convert local times to UTC on save and to display local times on the public page. |
| `StartDateUtc` | `datetime2` | UTC equivalent of `StartDate`, computed by the application on save: `TimeZoneInfo.ConvertTimeToUtc(StartDate, timeZone)`. Used for the upcoming/past split query (`StartDateUtc >= UtcNow`) and for chronological sorting. Must not be set by the client — always derived from `StartDate` + `TimeZoneId`. |
| `EndDateUtc` | `datetime2?` | UTC equivalent of `EndDate`, computed on save. Null when `EndDate` is null. |
| `Location` | `nvarchar(512)` | Required |
| `ExternalUrl` | `nvarchar(2048)?` | Optional HTTPS link to event site; `http://` URLs are rejected at the validation layer |
| `Published` | `bit` | `false` by default; toggle for visibility |
| `CreatedAt` | `datetime2` | UTC, set on insert |
| `UpdatedAt` | `datetime2` | UTC, updated on save including publish/unpublish state changes |
| `Version` | `int` | Optimistic concurrency; starts at `1` on first insert, incremented by `1` on each update |

**Recommended indexes**: `IX_Events_Published_StartDateUtc` on `(Published, StartDateUtc)` — supports the upcoming/past query which filters on `Published` and sorts by `StartDateUtc`. `IX_Events_Slug` unique on `Slug`.

**`EndDate >= StartDate` constraint**: There is **no DB-level CHECK constraint** for this rule. The `EndDate >= StartDate` invariant is enforced exclusively at the application layer via `CreateEventCommandValidator` and `UpdateEventCommandValidator` (FluentValidation `Must` predicate — see §3.2). A developer writing `IEntityTypeConfiguration<Event>` must **not** add `CHECK (EndDate >= StartDate)` to the migration. Application-layer enforcement is sufficient here: the back-office UI is the only write path, all writes pass through the validator before the command reaches EF Core, and a CHECK constraint in the DB would add migration complexity with no correctness benefit given the single-writer design.

---

## 5. Key Workflows

### 5.1 Create Event

![Create Event Sequence](diagrams/sequence_create_event.png)

Key points:
- New events are always created with `published=false` (draft state).
- The slug is generated from the title and checked for uniqueness before insert.
- Required fields: `Title`, `StartDate`, `Location` (L2-064.3).
- Validation: if `EndDate` is provided it must be ≥ `StartDate`; returns 400 otherwise.
- The handler computes `StartDateUtc` (and `EndDateUtc` if provided) from the local times and `TimeZoneId` before persisting.
- `ExternalUrl`, if provided, must be a well-formed absolute HTTPS URL (`https://` only); `http://` URLs are rejected with 400. Returns 400 for malformed values.

### 5.2 Publish / Unpublish Event

![Publish Event Sequence](diagrams/sequence_publish_event.png)

Key points:
- Publish and unpublish are separate endpoints rather than a single toggle, making intent explicit.
- Once published, the event appears on `/events` in the appropriate upcoming or past section based on `StartDate` relative to the current UTC time.
- Unpublishing an event removes it from the public site immediately.
- Both `PublishEventHandler` and `UnpublishEventHandler` call `ICacheInvalidator` to bust the public events cache after the state change.
- **Idempotency**: Both endpoints are idempotent. Calling `POST /api/events/{id}/publish` on an already-published event returns `200 + EventDto` without error; calling `POST /api/events/{id}/unpublish` on an already-unpublished event likewise returns `200 + EventDto`. This prevents spurious 409 responses when a client retries due to a network timeout and avoids requiring the caller to check current state before acting. `ICacheInvalidator` is only called when the state actually changes, not on no-op invocations.

---

## 6. API Contracts

### Back-office endpoints (require `Authorization: Bearer <token>`)

| Method | Path | Body / Params | Success | Errors |
|--------|------|---------------|---------|--------|
| `POST` | `/api/events` | `{ title, description, startDate, location, timeZoneId, endDate?, externalUrl? }` | 201 + `EventDto` | 400, 401, 409 |
| `PUT` | `/api/events/{id}` | `{ title, description, startDate, location, timeZoneId, endDate?, externalUrl?, version }` | 200 + `EventDto` | 400, 401, 404, 409 |
| `DELETE` | `/api/events/{id}` | — | 204 | 401, 404, 409 |
| `POST` | `/api/events/{id}/publish` | `{ version }` | 200 + `EventDto` | 401, 404, 409 |
| `POST` | `/api/events/{id}/unpublish` | `{ version }` | 200 + `EventDto` | 401, 404, 409 |
| `GET` | `/api/events/{id}` | — | 200 + `EventDto` | 401, 404 |
| `GET` | `/api/events?page&pageSize` (default pageSize=20, max 50) | — | 200 + `PagedResponse<EventListDto>` | 401 |

**Published filter**: The back-office list intentionally does **not** accept a `published` boolean filter. `EventListDto` includes the `published` field so the author can see state inline; the back-office UX is expected to be a small list where client-side filtering is sufficient. A server-side `?published=true/false` filter is not needed at this scale and is intentionally omitted to keep the endpoint surface minimal. If the list grows large enough to warrant server-side filtering, a `published` boolean query parameter can be added to `GetEventsQuery` and `EventRepository.GetAllAsync` without a breaking change.

**Version requirement on state transitions**: `POST .../publish` and `POST .../unpublish` now require `{ version }` in the JSON request body, matching the concurrency model used by `PUT /api/events/{id}`. The `version` field uses the same `GreaterThan(0)` validation as the update endpoint. Clients that omit `version` receive a 400 response. The back-office UI must include the current event version (from the most recent `EventDto` response) in every publish/unpublish request. This prevents a stale tab from accidentally publishing an event that another tab has since modified.

### Public endpoints (no auth)

| Method | Path | Params | Success | Errors |
|--------|------|--------|---------|--------|
| `GET` | `/api/events/published` | `?upcomingPage&pastPage&pageSize` (default upcomingPage=1, pastPage=1, pageSize=20, max pageSize=50) | 200 + `PublicEventsDto` / 304 | — |

**`pageSize` applies independently to both sections**: a single `pageSize` value controls the page size for both the upcoming and past sections. A request with `upcomingPage=1&pastPage=1&pageSize=5` returns up to 5 upcoming events **and** up to 5 past events — a maximum of 10 items in total. This is intentional: the two sections are rendered as separate lists on the public page and are paginated independently; using a single `pageSize` parameter keeps the API surface minimal while allowing both sections to be navigated with consistent page sizes. Callers that need asymmetric page sizes (e.g. show 3 upcoming but 10 past) cannot express this with the current API — this is an accepted limitation at current scale.
| `GET` | `/api/events/by-slug/{slug}` | — | 200 + `PublicEventDto` / 304 | 404 |

### DTOs

```
EventDto         { eventId, title, slug, description, startDate, endDate?, timeZoneId, startDateUtc, endDateUtc?, location, externalUrl?, published, createdAt, updatedAt, version }
EventListDto     { eventId, title, slug, startDate, location, timeZoneId, published }  // slug is guaranteed non-null: event slugs are generated at create time (unlike Newsletter slugs which are null until send); every Event row always has a slug value
PublicEventDto   { title, slug, description, startDate, endDate?, timeZoneId, startDateUtc, location, externalUrl? }  // eventId omitted — public consumers navigate by slug; exposing PK enables GUID enumeration. Strips internal fields (published, version, updatedAt)
PublicEventsDto  { upcoming: PagedResponse<PublicEventDto>, past: PagedResponse<PublicEventDto> }
```

---

## 7. Security Considerations

- **Authorization**: All write endpoints (`POST`, `PUT`, `DELETE`, publish/unpublish) are protected by `[Authorize]` and the JWT middleware. The back-office list endpoint also requires auth (L2-067.3).
- **Slug stability**: Event slugs are frozen after first publish. While in draft (never published), the slug is regenerated from the title on each update. Once `FirstPublishedAt` is set (first publish), the slug becomes immutable — title changes on subsequent updates do not affect the slug. Published event URLs are therefore stable for SEO indexing, social sharing, and bookmarks. The `ExternalUrl` field (when available) provides an additional stable external reference to the event site.
- **ExternalUrl HTTPS enforcement**: `ExternalUrl`, when provided, must be a well-formed absolute HTTPS URL (`https://` scheme only). `http://` URLs are rejected with 400 before any persistence occurs. Allowing insecure `http://` links would lower the security baseline for public event content — visitors clicking an `http://` link from a blog served over HTTPS would trigger mixed-content warnings in some browsers and could be subject to man-in-the-middle interception. **Development/test exception**: In non-production environments (`ASPNETCORE_ENVIRONMENT != Production`), `http://` URLs may be permitted to simplify local testing with `http://localhost` event URLs. This exception must be implemented as a configuration toggle (e.g. `EventOptions:AllowHttpExternalUrl`, default `false`) checked by the validator, not by weakening the validator itself. The toggle must not be enabled in production deployments.
- **ExternalUrl migration**: If the feature has already been implemented with `http://` URLs in the database, a one-time data migration should update existing rows: for each event with an `ExternalUrl` starting with `http://`, attempt to rewrite to `https://`. Events where the HTTPS version is unreachable (e.g. the external site does not support HTTPS) should have their `ExternalUrl` set to `null` and the author notified to provide an updated link. Until migration is complete, the validator must not reject reads of existing `http://` rows — only writes (create/update) enforce the HTTPS requirement. If the feature has not yet been implemented (current state), no migration is needed.
- **Input validation**: `EndDate`, when provided, must be ≥ `StartDate` (validated in the command handler). `ExternalUrl`, when provided, must be a well-formed absolute HTTPS URL; malformed values are rejected with 400 before any persistence occurs.
- **Delete guard**: Deleting a published event returns 409. The author must unpublish first, making removal from the public site an explicit step. This feature uses hard delete (physical row removal) rather than the status-based immutability used by Newsletter (Sent newsletters are never deleted) or the history-table approach used by About. The divergence is intentional: events have no audit or archive obligation and can legitimately be removed once they are no longer relevant, provided they are first hidden from public view via unpublish.
- **404 vs 410 after hard delete**: Once an event is hard-deleted, any request to `GET /api/events/by-slug/{slug}` (or the Razor Page `/events/{slug}`) returns 404 Not Found. A search crawler that had previously indexed the event's URL will re-request it and receive 404; HTTP 410 Gone would be semantically more correct (permanently removed, do not re-index), but this requires retaining a tombstone record or a separate deleted-slugs table. For a personal blog the SEO impact is low and the implementation cost is high, so 404 is accepted. If the author cares about de-indexing speed, the recommended workaround is to first set a `<meta name="robots" content="noindex">` via an unpublished but still-accessible state, wait for the next crawl, then delete. This limitation is documented here for future consideration.
- **Unpublished event access**: `GetEventBySlugHandler` explicitly checks `Published == true` before returning the event, ensuring draft events are not accessible to public visitors (L2-070.2).
- **HTTP caching**: The `GET /api/events/published` and `GET /api/events/by-slug/{slug}` endpoints are served with `Cache-Control: public, max-age=60, stale-while-revalidate=300`. Publish and unpublish operations must call `ICacheInvalidator` to bust the public events cache. `UpdateEventHandler` must also call `ICacheInvalidator` when the event being updated is currently published, so that title, date, or location changes are reflected immediately. **Slug change invalidation**: because the slug is regenerated from the title on every update (see OQ1), a title change produces a new slug. When this occurs, `UpdateEventHandler` must invalidate **both** the old slug's cache entry (`/api/events/by-slug/{oldSlug}`) and the published-list cache entry (`/api/events/published`). The old slug must be read from the existing entity before the update is applied so that the stale cache key is known. Failing to invalidate the old slug leaves a ghost cache entry that serves a 200 response for up to `max-age=60` seconds after the slug has changed, which is incorrect (a subsequent request for the old slug once the cache expires will return 404). **ETag / conditional GET**: Both public event endpoints support conditional requests to avoid transferring unchanged response bodies on revalidation.

  - **`GET /api/events/by-slug/{slug}`**: The response includes a `Last-Modified` header set to the event's `UpdatedAt` timestamp (truncated to whole seconds per RFC 7232 §2.2) and a weak `ETag` header: `W/"{slug}:{Version}"` (e.g. `W/"my-event:3"`). On subsequent requests, clients (or reverse proxies) send `If-None-Match: W/"my-event:3"` or `If-Modified-Since: <timestamp>`. The handler checks: if the ETag matches the current `slug:Version`, or if `UpdatedAt` ≤ `If-Modified-Since`, it returns **304 Not Modified** with no body. Otherwise it returns the full 200 response with updated headers. The weak ETag uses the `W/` prefix so that compressed and uncompressed representations share the same validator (per RFC 7232 §2.1).
  - **`GET /api/events/published`**: The response includes a `Last-Modified` header set to the maximum `UpdatedAt` across all currently published events (or the current time if no events are published). The weak `ETag` is `W/"pub:{maxVersion}:{count}"` where `maxVersion` is the highest `Version` among published events and `count` is the total number of published events — a change to any event's content (version bump) or a publish/unpublish operation (count change) produces a new ETag. On conditional request match, the endpoint returns **304 Not Modified**.
  - **Integration with `Cache-Control`**: The `Cache-Control: public, max-age=60, stale-while-revalidate=300` policy remains unchanged. During the `max-age` window, clients serve from cache without contacting the origin. After `max-age` expires, clients revalidate using `If-None-Match` / `If-Modified-Since` — if the content has not changed, the 304 response avoids transferring the full body. During the `stale-while-revalidate` window, clients may serve stale content while performing background revalidation. `ICacheInvalidator` still purges the server-side cache on publish/unpublish/update; the conditional GET headers complement (not replace) active invalidation.
  - **Razor Pages**: The public Razor Pages (`/events` and `/events/{slug}`) should emit matching `Last-Modified` and `ETag` headers using the same derivation logic, enabling browser conditional requests on page navigation. The middleware pipeline must check conditional headers **before** rendering the Razor view to avoid wasting render cycles on 304 responses.
- **Optimistic concurrency — version-in-body**: All state-changing endpoints (`PUT /api/events/{id}`, `POST /api/events/{id}/publish`, `POST /api/events/{id}/unpublish`) require `version` in the request body. The handler compares `command.Version` against `storedEvent.Version`; a mismatch returns 409 Conflict to prevent lost-update and stale-state-transition conflicts. This is consistent across all three endpoints — the same concurrency model applies to field updates, publish, and unpublish. The version-in-body approach (rather than ETag/If-Match) is an intentional deviation consistent with the Newsletter feature: it is simpler with MediatR commands and does not require clients to manage opaque header values. The back-office client must always use the `version` from the most recent response `EventDto` when issuing subsequent state-changing requests. After any 409, the client must re-fetch the event to obtain the current version before retrying.
- **Input length validation**: Command handlers enforce DB column limits as server-side validation: `Title` ≤ 256 chars, `Description` ≤ 4000 chars (event blurb; `nvarchar(max)` is retained in the schema for migration flexibility but the application layer enforces this cap — equivalent to the Kestrel 1 MB limit applied to Newsletter/About `Body`), `Location` ≤ 512 chars, `ExternalUrl` ≤ 2048 chars. Requests exceeding these limits are rejected with 400 before any persistence occurs.
- **Pagination bounds**: `page`, `upcomingPage`, and `pastPage` must be ≥ 1 and `pageSize` must be ≥ 1 and ≤ 50. Requests outside these bounds are rejected with 400. Zero or negative values cause division-by-zero or negative offsets in pagination math and must not be forwarded to the repository.
- **Content-Type enforcement**: All `POST` and `PUT` endpoints must require `Content-Type: application/json`. Requests with a missing or mismatched `Content-Type` are rejected with 415 (Unsupported Media Type). This prevents silent null-model-binding failures when clients send `application/x-www-form-urlencoded` or other content types.
- **Slug generation failure**: If `ISlugGenerator` produces an empty or blank slug (e.g. `Title` contains only non-ASCII characters), `CreateEventHandler` and `UpdateEventHandler` (when `FirstPublishedAt` is null) must fall back to the event's `EventId` (formatted as a lowercase hex string without hyphens) as the slug, ensuring uniqueness without failing the operation. This fallback cannot occur after first publish because the slug is frozen.
- **Time zone validation**: `TimeZoneId` is validated against `TimeZoneInfo.FindSystemTimeZoneById` — invalid or unsupported zone identifiers are rejected with 400. The IANA database is updated periodically with .NET runtime updates; zone IDs that were valid at event creation time remain valid for display. If a zone ID is retired in a future IANA update (extremely rare), the event's local times remain stored as-is — only UTC recomputation would be affected, and this can be handled by a one-time migration.
- **Observability**: Key operations must emit structured log events at `Information` level: `event.created` (eventId, title), `event.published` (eventId), `event.unpublished` (eventId), `event.deleted` (eventId). Errors (slug conflict, optimistic concurrency failure) are logged at `Warning` level.
- **Razor Page error handling**: The public `/events` Razor Page (list) and `/events/{slug}` Razor Page (detail) call the API layer via MediatR directly (same process), so a complete API failure is unlikely in isolation. However, if `GetPublishedEventsHandler` or `GetEventBySlugHandler` throws an unhandled exception (e.g. database unavailable), the Razor Pages must catch the exception and render a user-friendly error page (HTTP 500) rather than exposing a stack trace. The `/events/{slug}` page must return HTTP 404 when `GetEventBySlugHandler` returns null (event not found or not published), consistent with L2-070.2. The stale-while-revalidate window (`max-age=60, stale-while-revalidate=300`) provides a degree of resilience — a brief database outage may be masked by the CDN/reverse-proxy serving stale responses.

---

## 8. Open Questions

1. **Slug regeneration on update**: ~~Consider freezing the slug after first publish.~~ **Resolved**: The slug is frozen after first publish. While an event has never been published (`FirstPublishedAt` is null), `UpdateEventHandler` regenerates the slug from the current title on each update, allowing the author to iterate on the title during drafting. Once the event is published for the first time (`PublishEventHandler` sets `FirstPublishedAt = UtcNow`), the slug becomes immutable — subsequent title changes do not affect the slug. This ensures published event URLs remain stable for SEO, bookmarks, and social sharing. If an author needs a different slug after publish, they must delete the event and create a new one (or a future admin endpoint could provide explicit slug override with redirect support). `FirstPublishedAt` is never cleared on unpublish, so an event that was published, unpublished for editing, and re-published retains its original slug throughout.
2. **Pagination on public events**: ~~L2-069 does not mention pagination.~~ **Resolved**: Pagination is added to the public events page. Both the upcoming and past sections are paginated independently. `GetPublishedEventsQuery` accepts `upcomingPage`, `pastPage`, and `pageSize` parameters. The public API response is updated to `{ upcoming: PagedResponse<PublicEventDto>, past: PagedResponse<PublicEventDto> }`.
3. **Time zone handling**: ~~Should the time zone be a field on the event, a site-wide setting, or always UTC?~~ **Resolved**: Each event stores a `TimeZoneId` (IANA identifier) alongside `StartDate`/`EndDate` (venue-local wall-clock times). UTC equivalents (`StartDateUtc`, `EndDateUtc`) are computed by the application on save for efficient querying and sorting. The public page renders dates in the venue's local time zone with the zone abbreviation (e.g. "Mar 15, 2026 7:00 PM EST"). **DST handling**: `TimeZoneInfo.ConvertTimeToUtc` correctly handles DST transitions — if the author enters a local time that falls in a DST gap (e.g. 2:30 AM during spring-forward), the framework adjusts to the nearest valid time. If the local time is ambiguous (fall-back), the standard time interpretation is used. These edge cases are handled by .NET's `TimeZoneInfo` and require no special application logic. **All-day events**: Not explicitly modeled — authors enter a start time of midnight and an end time of 23:59 (or omit `EndDate`). A future enhancement could add an `IsAllDay` flag.
