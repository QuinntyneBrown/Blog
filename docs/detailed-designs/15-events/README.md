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
  - `GET /api/events/public` — public: returns upcoming + past published events (no auth)
  - `GET /api/events/{slug}` — public event detail by slug (no auth)

### 3.2 Command Handlers

| Handler | Command | Effect |
|---------|---------|--------|
| `CreateEventHandler` | `CreateEventCommand` | Generates slug, persists with `published=false` |
| `UpdateEventHandler` | `UpdateEventCommand` | Updates all mutable fields; regenerates slug from new title |
| `DeleteEventHandler` | `DeleteEventCommand` | Removes event regardless of published state |
| `PublishEventHandler` | `PublishEventCommand` | Sets `published=true` |
| `UnpublishEventHandler` | `UnpublishEventCommand` | Sets `published=false` |

### 3.3 Query Handlers

| Handler | Query | Returns |
|---------|-------|---------|
| `GetEventsHandler` | `GetEventsQuery(page, pageSize)` | Paginated list, all statuses, descending by `StartDate` |
| `GetPublishedEventsHandler` | `GetPublishedEventsQuery` | `{ upcoming: Event[], past: Event[] }` split by `StartDate` vs `UtcNow` |
| `GetEventBySlugHandler` | `GetEventBySlugQuery(slug)` | Single published event; 404 if not found or not published |
| `GetEventByIdHandler` | `GetEventByIdQuery(eventId)` | Admin detail by ID |

### 3.4 EventRepository

- **Responsibility**: Data access for `Event` entities.
- **Key methods**:
  - `GetByIdAsync(eventId)` — by PK
  - `GetBySlugAsync(slug)` — by unique slug
  - `GetAllAsync(page, pageSize)` — paginated, all statuses, ordered by `StartDate DESC`
  - `GetUpcomingAsync()` — published, `StartDate >= UtcNow`, ordered `StartDate ASC`
  - `GetPastAsync()` — published, `StartDate < UtcNow`, ordered `StartDate DESC`
  - `SlugExistsAsync(slug, excludeEventId?)` — uniqueness check

### 3.5 ISlugGenerator

- Reused from existing infrastructure (`SlugGenerator`). Converts `Title` to a URL-safe slug. Called on both create and update.
- On update, the slug is regenerated from the new title. If the new slug conflicts with a **different** event, the handler returns 409 (L2-065.2).

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
| `Description` | `nvarchar(max)` | Required |
| `StartDate` | `datetime2` | Required; drives upcoming/past split |
| `EndDate` | `datetime2?` | Optional |
| `Location` | `nvarchar(512)` | Required |
| `ExternalUrl` | `nvarchar(2048)?` | Optional link to event site |
| `Published` | `bit` | `false` by default; toggle for visibility |
| `CreatedAt` | `datetime2` | UTC, set on insert |
| `UpdatedAt` | `datetime2` | UTC, updated on save |
| `Version` | `int` | Optimistic concurrency |

---

## 5. Key Workflows

### 5.1 Create Event

![Create Event Sequence](diagrams/sequence_create_event.png)

Key points:
- New events are always created with `published=false` (draft state).
- The slug is generated from the title and checked for uniqueness before insert.
- Required fields: `Title`, `StartDate`, `Location` (L2-064.3).

### 5.2 Publish / Unpublish Event

![Publish Event Sequence](diagrams/sequence_publish_event.png)

Key points:
- Publish and unpublish are separate endpoints rather than a single toggle, making intent explicit.
- Once published, the event appears on `/events` in the appropriate upcoming or past section based on `StartDate` relative to the current UTC time.
- Unpublishing an event removes it from the public site immediately.

---

## 6. API Contracts

### Back-office endpoints (require `Authorization: Bearer <token>`)

| Method | Path | Body / Params | Success | Errors |
|--------|------|---------------|---------|--------|
| `POST` | `/api/events` | `{ title, description, startDate, location, endDate?, externalUrl? }` | 201 + `EventDto` | 400, 401, 409 |
| `PUT` | `/api/events/{id}` | `{ title, description, startDate, location, endDate?, externalUrl?, version }` | 200 + `EventDto` | 400, 401, 404, 409 |
| `DELETE` | `/api/events/{id}` | — | 204 | 401, 404 |
| `POST` | `/api/events/{id}/publish` | — | 200 | 401, 404 |
| `POST` | `/api/events/{id}/unpublish` | — | 200 | 401, 404 |
| `GET` | `/api/events?page&pageSize` | — | 200 + `PagedResponse<EventListDto>` | 401 |

### Public endpoints (no auth)

| Method | Path | Params | Success | Errors |
|--------|------|--------|---------|--------|
| `GET` | `/api/events/public` | — | 200 + `PublicEventsDto` | — |
| `GET` | `/api/events/{slug}` | — | 200 + `EventDto` | 404 |

### DTOs

```
EventDto         { eventId, title, slug, description, startDate, endDate?, location, externalUrl?, published, createdAt, updatedAt, version }
EventListDto     { eventId, title, slug, startDate, location, published }
PublicEventsDto  { upcoming: EventDto[], past: EventDto[] }
```

---

## 7. Security Considerations

- **Authorization**: All write endpoints (`POST`, `PUT`, `DELETE`, publish/unpublish) are protected by `[Authorize]` and the JWT middleware. The back-office list endpoint also requires auth (L2-067.3).
- **Slug immutability**: Unlike articles (where the slug is frozen on publish), event slugs are regenerated on every update from the current title. This is intentional — events rarely change title, and the external URL (if provided) is the canonical reference. If stable public URLs are needed, slug freezing should be added.
- **Unpublished event access**: `GetEventBySlugHandler` explicitly checks `Published == true` before returning the event, ensuring draft events are not accessible to public visitors (L2-070.2).

---

## 8. Open Questions

1. **Slug regeneration on update**: Regenerating the slug on every edit can break bookmarked public URLs. Consider freezing the slug after first publish (matching the Article pattern). Decision needed.
2. **Pagination on public events**: L2-069 does not mention pagination for the public events page (it splits by upcoming/past). If the event count grows large, pagination or a count cap may be needed.
3. **Time zone handling**: `StartDate` is stored as UTC. The public display likely needs to render in the author's local time zone. Should the time zone be a field on the event, a site-wide setting, or always displayed as UTC?
