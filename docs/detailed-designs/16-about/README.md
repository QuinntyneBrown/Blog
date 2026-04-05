# About Page — Detailed Design

## 1. Overview

The About Page feature provides a publicly visible biography for the site author. A single authenticated user can author and update the about page content — consisting of a heading, a rich-text body (Markdown), and an optional profile image from the digital asset library. The public `/about` page renders the content with appropriate SEO metadata and HTTP caching headers.

### Requirements Traceability

| Requirement | Description |
|-------------|-------------|
| L1-016 | About page: public display and back-office content authoring |
| L2-071 | Public about page display (`/about`) |
| L2-072 | About content management (back office) |
| L2-073 | About page SEO |

### Actors

- **Anonymous Visitor** — reads the about page at `/about`
- **Blog Author** — authors and updates the about page content through the back-office editor

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

### 3.1 AboutController

- **Responsibility**: Public GET, authenticated PUT, and back-office history/restore for the about content.
- **Interfaces**:
  - `GET /api/about` — returns current about content (anonymous, response-cached)
  - `PUT /api/about` — creates or updates about content (requires `[Authorize]`)
  - `GET /api/about/history` — paginated revision history (requires `[Authorize]`)
  - `PUT /api/about/restore/{historyId}` — restore a prior revision (requires `[Authorize]`)
- **Caching**: The public GET response is served with `Cache-Control: public, max-age=60, stale-while-revalidate=600` (L2-071.3) **only when about content exists**. When `GET /api/about` returns a `null` body (no content has been authored yet), the response must be served with `Cache-Control: no-store` so that reverse proxies and CDNs do not cache the absence of content. If the null response were cached with `public, max-age=60`, a CDN edge would serve "no content" for up to 60 seconds after the author's first save — and the `ICacheInvalidator` call issued on that first save may not reach all CDN edges, leaving the empty-state response stranded in edge caches. By returning `no-store` for the null case, the first real save is visible immediately without relying on cache invalidation. The `ICacheInvalidator` is called on a successful PUT or restore to bust the cached response. `GET /api/about` does not emit an ETag or Last-Modified header; clients cannot perform conditional requests (`If-None-Match` / `If-Modified-Since`). This is acceptable for a singleton endpoint with a 60-second TTL. If conditional GET support is added in future, `Last-Modified` should be derived from `AboutContent.UpdatedAt`.
- **Empty state**: If no about content has been authored, `GET /api/about` returns `null` in the body with HTTP 200. The public Razor Page renders a default message (L2-071.2). `404` is intentionally avoided here — a 404 on a well-known singleton endpoint would look like a routing or server error rather than a legitimate empty-content state, and would require special-casing in the Razor Page and any API client that checks for 404 vs 200. Returning `200 + null` makes the absence of content a normal, handled state rather than an error. Clients should treat a null body as "no content yet authored" and render an empty state accordingly.

### 3.2 UpsertAboutContentHandler

- **Validator**: `UpsertAboutContentCommandValidator` (ADR-0005 FluentValidation pipeline behaviour; rules: `Heading` NotEmpty, MaximumLength(256); `Body` NotEmpty; `profileImageId` must be a non-empty `Guid` when provided (the validator rejects a zero-value `Guid` — `Guid.Empty` — because it can only arise from a serialisation error, not a legitimate asset reference); `version` rule: on the **update path** (existing record found) `version` must be ≥ 1 — a value of 0 is rejected with 400. On the **first-insert path** (no record exists), the client has no prior version and must send `version: 0`; the validator must allow `version = 0` in this case and the handler must not apply an optimistic concurrency check on insert. Clients always include `version` in the request body — the field is never omitted — but the value is `0` for the very first save and ≥ 1 for all subsequent saves.)
- **Responsibility**: Creates the about record if it does not exist; updates it if it does. This is the only mutation handler for about content.
- **Dependencies**: `AboutContentRepository`, `IMarkdownConverter`, `ICacheInvalidator`, `DigitalAssetRepository` (to validate `profileImageId` and verify asset ownership matches the requesting user)
- **Pre-render**: Markdown `Body` is converted to sanitized HTML at save time and stored in `BodyHtml`. Runtime rendering is not performed (same pattern as articles).
- **Timestamp management**: On the insert path, the handler must set `CreatedAt = UpdatedAt = UtcNow` and `Version = 1` explicitly. On the update path, the handler must set `UpdatedAt = UtcNow` before saving. `CreatedAt` must never be modified on the update path — it is the singleton creation date and must remain stable. When snapshotting the current live row into `AboutContentHistory` on the update path, the handler must set `ArchivedAt = UtcNow` on the history entity — EF Core does not auto-populate this field and there is no DB default specified for the column; leaving it unset would persist `DateTime.MinValue` (or the CLR default) rather than the actual snapshot time. Neither path should rely on a DB default or EF Core interceptor to populate these fields unless such an interceptor is explicitly configured in the application.
- **Optimistic concurrency**: On update, the command must include the current `version`. If the stored `Version` differs, the handler returns 409 (conflict). On success, `Version` is incremented before saving.
- **Cache invalidation**: After a successful upsert (both insert and update paths), the handler must call `ICacheInvalidator.InvalidateAsync("/about")` to evict the cached public GET response. This ensures visitors see updated content within the `stale-while-revalidate` window rather than waiting for the full `max-age=60` TTL to expire.
- **First-insert concurrency**: `UpsertAboutContentHandler` performs a read-then-write (check for existing row → insert if null, update if found). Two simultaneous first-ever saves will both read null and both attempt insert, causing a primary key violation on the second writer. The handler must catch the resulting `DbUpdateException` and retry the operation as an update (option a in §7). See **§7 — First-insert concurrency** for the full rationale and the alternative serializable-transaction approach.

### 3.3 RestoreAboutContentHandler

- **Validator**: `RestoreAboutContentCommandValidator` — FluentValidation rules: `HistoryId` NotEmpty (rejects `Guid.Empty`; the route binder rejects non-GUID values before the command is constructed, so a separate "valid GUID" rule is redundant); `CurrentVersion` GreaterThan(0) (a zero value indicates the caller has no live row to protect, which is an invalid request on the restore path — the live row always exists and always has version ≥ 1 by the time a restore is attempted)
- **Responsibility**: Reverts the live about content to a prior revision identified by `historyId`.
- **Dependencies**: `AboutContentRepository`, `IMarkdownConverter`, `ICacheInvalidator`
- **Steps**: (1) Load history record by `historyId`; return 404 if not found. (2) Verify `AboutContentId` matches the singleton — prevents cross-resource access. (3) Load the current live row; if the supplied `currentVersion` does not match the stored `Version`, return 409 (prevents a restore from silently overwriting concurrent edits). (4) Snapshot the current live row into `AboutContentHistory`; the handler must set `ArchivedAt = UtcNow` on the new history entity — EF Core does not auto-populate this field and there is no DB default for the column; leaving it unset would persist `DateTime.MinValue` rather than the actual snapshot time. (5) Overwrite live row with exactly the following fields from the history snapshot: `Heading`, `Body`, `ProfileImageId`; re-render `BodyHtml` by calling `IMarkdownConverter.Convert(historyRecord.Body)` rather than copying the snapshot's `BodyHtml` directly — this ensures the restored HTML is produced under the current sanitizer allow-list rather than stale rules that may have been in effect when the snapshot was originally saved; set `UpdatedAt = UtcNow`; increment `Version`. `CreatedAt` and `AboutContentId` must NOT be copied from the snapshot — `CreatedAt` is the singleton's original creation date and must remain stable; `AboutContentId` is the PK and must never change. (6) Call `ICacheInvalidator.InvalidateAsync("/about")`. Steps 4 and 5 must execute within a single DB transaction so that a failure between them cannot produce an orphaned history snapshot with no corresponding live-row update.
- **Double-invoke behaviour**: If the same restore is called twice concurrently (e.g. a double-click from the browser), the first call will succeed. The second call will fail at step 3 with 409 because the live row's `Version` was incremented by the first call — the `currentVersion` in the second request no longer matches. This is the correct and safe outcome: the optimistic concurrency check prevents a spurious second history snapshot from being written. No additional guard is needed beyond the existing version check.

### 3.4 GetAboutHistoryHandler

- **Responsibility**: Returns a paginated list of `AboutContentHistory` records ordered by `ArchivedAt DESC`.
- **Dependencies**: `AboutContentRepository`

### 3.5 GetAboutContentHandler

- **Responsibility**: Retrieves the current about content record (singleton). Returns `null` if no record exists.
- **Returns**: `PublicAboutContentDto?` — the handler projects onto `PublicAboutContentDto` (omitting `body`, `aboutContentId`, `createdAt`, `updatedAt`, and `version`), which is the DTO returned by `GET /api/about`. The back-office editor does not use this handler; it re-uses the upsert response (`AboutContentDto`) which includes `body`, `version`, and all audit fields needed for subsequent edits.
- **Dependencies**: `AboutContentRepository`, `DigitalAssetRepository` (resolves profile image URL from `ProfileImageId`)

### 3.6 AboutContentRepository

- **Responsibility**: Single-row data access for `AboutContent`.
- **Key methods**:
  - `GetCurrentAsync()` — `FirstOrDefaultAsync()` with a **LEFT JOIN** to the `DigitalAssets` table on `ProfileImageId`. The join must be a LEFT (outer) join — not an INNER join — so that an `AboutContent` row whose `ProfileImageId` is null, or whose referenced asset has since been deleted, is still returned. An INNER join would silently return null when the profile image asset is missing, hiding the entire about content from visitors as if no content existed.
  - `AddAsync(entity)` — insert
  - `UpdateAsync(entity)` — update (caller increments version)
- **Notes**: No ID-based lookup needed — there is always at most one row. No `GetAllAsync` or pagination.
- **History methods**: `AddHistoryAsync(entity)` — inserts snapshot into `AboutContentHistory`. `GetHistoryAsync(page, pageSize)` — paginated history ordered by `ArchivedAt DESC`. `GetHistoryByIdAsync(historyId)` — single snapshot by PK for restore operations.

---

## 4. Data Model

### 4.1 Class Diagram

![Class Diagram](diagrams/class_diagram.png)

### 4.2 Entity Descriptions

#### AboutContent

| Field | Type | Notes |
|-------|------|-------|
| `AboutContentId` | `Guid` | PK |
| `Heading` | `nvarchar(256)` | Required; used as `<h1>` and SEO `<title>` |
| `Body` | `nvarchar(max)` | Raw Markdown source |
| `BodyHtml` | `nvarchar(max)` | Pre-rendered, sanitized HTML |
| `ProfileImageId` | `Guid?` | FK → `DigitalAssets.DigitalAssetId`; nullable |
| `CreatedAt` | `datetime2` | UTC, set on first insert |
| `UpdatedAt` | `datetime2` | UTC, updated on save |
| `Version` | `int` | Optimistic concurrency; starts at `1` on first insert, incremented by `1` on each update |

**Design note**: The table is treated as a singleton. `UpsertAboutContentHandler` calls `GetCurrentAsync()` — if null, it inserts; otherwise it copies the current row to `AboutContentHistory` then updates within a single DB transaction. No unique constraints beyond the PK are needed.

**Initial version value**: The `Version` field is set to `1` on the first insert (not `0`). This makes the first PUT after creation intuitive: the client receives `version: 1` in the `200` response and sends `version: 1` back on the first update. Using `0` for the initial insert would require clients to distinguish a "never-updated" state from a standard version counter, which adds unnecessary complexity. The `Version` in `AboutContentHistory` snapshots reflects the version number copied from the parent at the time of the snapshot — so the history row for the first edit will carry `version: 1`, corresponding to the state being replaced.

**Upsert response code**: `PUT /api/about` returns `200` for both the first-ever insert and subsequent updates. REST convention suggests `201 Created` for an initial insert via PUT, but since the About content is a singleton and clients do not discover the URL from a Location header, `200` is simpler and consistent. A client that needs to distinguish create from update can compare `createdAt == updatedAt` in the response body (they are equal on first insert and diverge on subsequent saves).

#### AboutContentHistory

| Field | Type | Notes |
|-------|------|-------|
| `AboutContentHistoryId` | `Guid` | PK |
| `AboutContentId` | `Guid` | FK → `AboutContent.AboutContentId` |
| `Heading` | `nvarchar(256)` | Snapshot at time of save |
| `Body` | `nvarchar(max)` | Raw Markdown snapshot |
| `BodyHtml` | `nvarchar(max)` | Pre-rendered HTML snapshot — stored for preview rendering in the history list UI. **Not used verbatim on restore**: `RestoreAboutContentHandler` (§3.3 step 5) re-renders `BodyHtml` from `Body` via `IMarkdownConverter` at restore time so that the restored HTML reflects the current sanitizer allow-list rather than rules that may have been in effect when the snapshot was originally saved. Implementers must not "optimise" restore by copying this field directly. |
| `ProfileImageId` | `Guid?` | Snapshot value — **no FK constraint**. Storing this as a live FK to `DigitalAssets` would cause `ON DELETE RESTRICT` to block asset deletion whenever a history snapshot references that asset. Since history rows are immutable records of past state, the `ProfileImageId` value here is informational only; the corresponding asset may no longer exist. |
| `Version` | `int` | Version number copied from parent at time of snapshot |
| `ArchivedAt` | `datetime2` | UTC timestamp when this snapshot was written (i.e. when the previous live state was captured). This is **not** the timestamp of the original content creation — `AboutContent.CreatedAt` holds that value. `ArchivedAt` advances each time a new snapshot is taken (on every successful upsert and on every restore). |

**Recommended index**: `IX_AboutContentHistory_AboutContentId_ArchivedAt` on `(AboutContentId, ArchivedAt DESC)` to support the paginated history query efficiently.

**Cascade delete**: The FK from `AboutContentHistory.AboutContentId` → `AboutContent.AboutContentId` must be configured with `ON DELETE CASCADE`. Because `AboutContent` is a singleton its row is never expected to be deleted in normal operation, but without cascade the constraint would block any future hard-delete of the singleton and leave orphaned history rows.

**Retention**: `AboutContentHistory` has no automatic retention policy. For a single-author blog the table is expected to remain small (one row per save) and unbounded growth is not a practical concern. No scheduled purge job is required. If the table does grow large (e.g. frequent programmatic saves), a manual or operator-triggered cleanup of rows older than a chosen threshold is sufficient. This is intentionally left as a low-priority operational task rather than a design obligation, in contrast to `NewsletterSendLog` (which grows proportional to subscriber count × newsletter count and warrants the documented 90-day retention job).

---

## 5. Key Workflows

### 5.1 Upsert About Content

![Upsert About Content Sequence](diagrams/sequence_upsert_about.png)

Key points:
- Validation rejects empty `Heading` or `Body` with 400 (L2-072.3).
- Unauthenticated PUT returns 401 (L2-072.4).
- On update (existing record), `version` in the request body must match the stored value; a mismatch returns 409.
- Markdown is converted and sanitized at save time; the public page reads `BodyHtml` directly.
- `profileImageId`, if provided, must reference an existing `DigitalAsset` owned by the requesting user — validated before save (L2-072.5). Returns 403 if the asset exists but belongs to a different user.
- On restore (`PUT /api/about/restore/{historyId}`), the caller must supply the current `version` of the live row in the request body. If the version mismatches, the handler returns 409 — this prevents a restore from silently overwriting a concurrent in-flight edit. The current live state is snapshotted into `AboutContentHistory` before overwriting, making the restore reversible. `ICacheInvalidator.InvalidateAsync("/about")` is called after restore.

### 5.2 Public About Page Render

1. Visitor navigates to `/about`.
2. The Razor Page dispatches `GetAboutContentQuery` via MediatR.
3. If no content exists, the page renders an empty-state message (HTTP 200, L2-071.2).
4. If content exists, the page renders `Heading` as `<h1>`, `BodyHtml` as the body, and the profile image (if set).
5. The `<head>` includes:
   - `<title>{Heading} — {SiteName}</title>` (L2-073.1)
   - `<meta name="description" content="...">` derived from `BodyHtml` stripped of HTML tags, truncated to ~160 chars (using raw `Body` Markdown would bleed syntax characters into the description)
   - `og:title` set to `{Heading}`, `og:description` set to the **same stripped-text value** used for `<meta name="description">` (strip `BodyHtml` tags, truncate to ~160 chars) to ensure consistency across social-card and search-engine rendering, `og:type` set to `profile` (L2-073.2)
   - `og:image` referencing the profile image URL when set (L2-073.3)
   - `<link rel="canonical" href="https://{siteHost}/about">` — the URL is a fixed singleton so the canonical never varies, but it must be emitted to prevent search engines from treating `/about`, `/about?ref=...` or other variants as duplicate URLs (L2-073)
6. `Cache-Control: public, max-age=60, stale-while-revalidate=600` is applied via the page's `[ResponseCache]` attribute.
7. **Error handling**: Because the Razor Page and the API layer run in the same process and communicate via MediatR (no HTTP hop), a total API failure is equivalent to an unhandled exception in `GetAboutContentHandler`. The Razor Page must wrap the MediatR dispatch in a try/catch; if an exception is thrown (e.g. the database is unreachable), it must render a generic error message with HTTP 500 rather than surfacing a stack trace. The `stale-while-revalidate=600` window on the cached response provides partial resilience: a reverse-proxy or CDN may serve a stale copy during a brief outage, deferring the user-visible error.

---

## 6. API Contracts

| Method | Path | Auth | Body / Params | Success | Errors |
|--------|------|------|---------------|---------|--------|
| `GET` | `/api/about` | None | — | 200 + `PublicAboutContentDto?` | — |
| `PUT` | `/api/about` | Bearer token | `{ heading, body, profileImageId?, version }` — `version` must be `0` on first-ever insert (no prior version exists); must be ≥ 1 (current stored version) on subsequent updates | 200 + `AboutContentDto` | 400, 401, 403, 409 |
| `GET` | `/api/about/history` | Bearer token | `?page&pageSize` (default pageSize=20, max 50) | 200 + `PagedResponse<AboutContentHistoryDto>` | 401 |
| `PUT` | `/api/about/restore/{historyId}` | Bearer token | `{ currentVersion }` | 200 + `AboutContentDto` | 401, 403, 404, 409 |

### DTOs

```
PublicAboutContentDto  {
    heading:         string,
    bodyHtml:        string,       // Pre-rendered HTML — the only field the public page renders
    profileImageId:  Guid?,
    profileImageUrl: string?       // Resolved CDN/storage URL
    // body (raw Markdown) is intentionally excluded — anonymous visitors need only the
    // rendered HTML; exposing raw Markdown to the public serves no purpose and leaks
    // authoring artefacts (syntax characters) into the API surface
}

AboutContentDto  {
    aboutContentId:  Guid,
    heading:         string,
    body:            string,       // Markdown source — included for the back-office editor
    bodyHtml:        string,       // Pre-rendered HTML
    profileImageId:  Guid?,
    profileImageUrl: string?,      // Resolved CDN/storage URL
    createdAt:       DateTime,
    updatedAt:       DateTime,
    version:         int
}

AboutContentHistoryDto  {
    aboutContentHistoryId: Guid,
    // aboutContentId is intentionally omitted: AboutContent is a singleton and the back-office
    // history UI has no need to identify which parent record a snapshot belongs to — there is
    // only ever one. Including it would expose the singleton PK to the client for no benefit.
    heading:               string,
    body:                  string,       // Markdown source snapshot
    bodyHtml:              string,       // Pre-rendered HTML snapshot (used for preview rendering in history list)
    profileImageId:        Guid?,
    profileImageUrl:       string?,      // Resolved CDN/storage URL for display in history list
    version:               int,
    archivedAt:            DateTime
}
```

**Public vs back-office DTO split**: `GET /api/about` (anonymous) returns `PublicAboutContentDto` — it omits `body` (raw Markdown), `aboutContentId`, `createdAt`, `updatedAt`, and `version`, none of which the public page needs. `PUT /api/about` (authenticated) returns the full `AboutContentDto` so the back-office editor can display the current Markdown source and version for the next optimistic concurrency check. `GET /api/about/history` returns `AboutContentHistoryDto` (authenticated only) which retains `body` for the restore preview UI.

---

## 7. Security Considerations

- **Authorization**: The `PUT /api/about`, `GET /api/about/history`, and `PUT /api/about/restore/{historyId}` endpoints are all protected by `[Authorize]` and the JWT middleware. Anonymous access to any of these returns 401 (L2-072.4). `GET /api/about` is public and unauthenticated.
- **XSS**: `BodyHtml` is produced via `IMarkdownConverter`, which uses HtmlSanitizer to strip disallowed tags and attributes before storage.
- **Image validation**: `profileImageId` is validated against the `DigitalAssets` table and must be owned by the requesting user. Returns 403 if the asset belongs to a different user. Prevents orphaned references and SSRF-style manipulation of the image URL.
- **Cache invalidation**: `ICacheInvalidator.InvalidateAsync("/about")` is called after both a successful PUT and a successful restore, ensuring visitors see updated content promptly.
- **Optimistic concurrency — version-in-body vs ETag/If-Match**: `PUT /api/about` and `PUT /api/about/restore/{historyId}` pass `version` / `currentVersion` in the request body rather than using the standard REST `ETag` response header + `If-Match` request header pattern (RFC 7232). This is an intentional deviation shared across all three features (Newsletter, Events, About): the version travels with the rest of the MediatR command payload, simplifying implementation and keeping the concurrency contract explicit in the request body. The trade-off is non-standard REST behaviour; this is accepted because the only client is the purpose-built back-office Razor Pages front end. A mismatch returns 409 to prevent lost-update conflicts when the author has the page open in two tabs simultaneously.
- **Input length validation**: `UpsertAboutContentHandler` enforces DB column limits server-side: `Heading` ≤ 256 chars. Requests exceeding this limit are rejected with 400 before any persistence occurs. `Body` is `nvarchar(max)` and is bounded instead by the Kestrel 1 MB request body limit (see §6-restful-api global config).
- **Pagination bounds**: `page` must be ≥ 1 and `pageSize` must be ≥ 1 and ≤ 50. Requests outside these bounds are rejected with 400. Zero or negative values cause division-by-zero or negative offsets in pagination math and must not be forwarded to the repository.
- **Content-Type enforcement**: The `PUT /api/about` and `PUT /api/about/restore/{historyId}` endpoints must require `Content-Type: application/json`. Requests with a missing or mismatched `Content-Type` are rejected with 415 (Unsupported Media Type). This prevents silent null-model-binding failures.
- **First-insert concurrency**: `UpsertAboutContentHandler` performs a read-then-write (check for existing row → insert if null, update if found). Two simultaneous first-ever saves will both read null and both attempt insert, causing a primary key violation on the second writer. The handler must either: (a) catch the resulting `DbUpdateException` and retry as an update, or (b) wrap the check and insert in a serializable DB transaction. Option (a) (catch-and-retry) is preferred as it avoids elevated isolation level overhead on every save.
- **`profileImageId` TOCTOU race**: `UpsertAboutContentHandler` validates `profileImageId` (calls `DigitalAssetRepository` to verify the asset exists and is owned by the requesting user) before the EF Core save. Because `ProfileImageId` has no enforced FK constraint at the database level (§4.2 — the FK is intentionally unguarded to allow asset deletion without cascade), the asset could be deleted between the validation step and the save, leaving a dangling reference in `AboutContent.ProfileImageId`. This race is accepted and documented: the window is sub-millisecond under normal operating conditions and the site displays no image (broken image at worst) rather than corrupting data. No additional guard is required. If the image URL resolver (`GetAboutContentHandler` / `DigitalAssetRepository`) returns `null` for a missing asset ID, the public page must render without a profile image rather than throwing.
- **Observability**: Key operations must emit structured log events at `Information` level: `about.upserted` (version), `about.restored` (historyId, restoredVersion). Cache invalidation failures are logged at `Warning` level with the cache key so operators can manually evict stale entries.

---

## 8. Open Questions

1. **Profile image ownership**: ~~Should the about profile image be restricted to assets uploaded by the authenticated user, or any asset in the library?~~ **Resolved**: Restricted to assets uploaded by the authenticated user. `UpsertAboutContentHandler` must verify that the referenced `DigitalAsset` is owned by the requesting user before accepting the `profileImageId`.
2. **History / versioning**: ~~The design stores only the current version.~~ **Resolved**: Revision history is supported via a separate `AboutContentHistory` table. On every successful upsert, `UpsertAboutContentHandler` copies the current row into `AboutContentHistory` before applying the update. A back-office endpoint (`GET /api/about/history`) lists past revisions and a `PUT /api/about/restore/{historyId}` allows the author to revert to a previous version. Restoring also snapshots the current state into history first, making the restore itself reversible.
3. **Cache key**: ~~The `/about` route needs to be added to the invalidation set.~~ **Resolved**: The `/about` route is added to the `ICacheInvalidator` invalidation set. `UpsertAboutContentHandler` calls `ICacheInvalidator.InvalidateAsync("/about")` after a successful upsert.
