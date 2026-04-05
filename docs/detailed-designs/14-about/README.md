# Feature 14: About Page

## 1. Overview

This feature delivers a publicly visible "About" page that presents biographical and professional information about the site author, along with a back-office interface for authoring and updating the content. The about page is a singleton content type — only one about record exists at any time.

The back-office provides an edit form for the heading, Markdown body, and an optional profile image (linked digital asset). The public site renders the content at `/about` with proper SEO metadata and caching.

### Requirements Traceability

| Requirement | Description |
|-------------|-------------|
| L1-014 | About Page |
| L2-054 | Public About Page Display |
| L2-055 | About Content Management |
| L2-056 | About Page SEO |

## 2. Data Model

### 2.1 AboutContent Entity

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| AboutContentId | Guid | PK, auto-generated | Unique identifier |
| Heading | string | Required, max 256 chars | Page heading / title |
| Body | string | Required | Markdown source content |
| BodyHtml | string | Required | Pre-rendered HTML from Markdown |
| ProfileImageId | Guid? | FK → DigitalAssets, nullable | Optional profile image |
| Version | int | Default 1, concurrency token | Optimistic concurrency control |
| CreatedAt | DateTime | UTC, auto-set | Record creation timestamp |
| UpdatedAt | DateTime | UTC, auto-set | Last modification timestamp |

### 2.2 Database Indexes

| Index | Columns | Purpose |
|-------|---------|---------|
| PK_AboutContents | AboutContentId | Primary key |

## 3. API Contracts

### 3.1 Get About Content

```
GET /api/about
Authorization: Bearer {token}

200 OK
Content-Type: application/json

{
  "aboutContentId": "...",
  "heading": "About Me",
  "body": "# Hello\n\nI am a developer...",
  "bodyHtml": "<h1>Hello</h1>\n<p>I am a developer...</p>",
  "profileImageId": "...",
  "profileImageUrl": "/assets/profile.webp",
  "version": 1,
  "createdAt": "2026-04-05T00:00:00Z",
  "updatedAt": "2026-04-05T00:00:00Z"
}

404 Not Found (if no about content exists yet)
```

### 3.2 Create or Update About Content

```
PUT /api/about
Authorization: Bearer {token}
Content-Type: application/json

{
  "heading": "About Me",
  "body": "# Hello\n\nI am a developer...",
  "profileImageId": "..."
}

200 OK
Content-Type: application/json

{
  "aboutContentId": "...",
  "heading": "About Me",
  "body": "...",
  "bodyHtml": "...",
  "profileImageId": "...",
  "profileImageUrl": "/assets/profile.webp",
  "version": 2,
  "createdAt": "2026-04-05T00:00:00Z",
  "updatedAt": "2026-04-05T12:00:00Z"
}

400 Bad Request (validation errors)
401 Unauthorized
```

## 4. Key Workflows

### 4.1 Author About Content (Back Office)

1. Admin navigates to `/admin/about`.
2. If about content exists, the form is pre-populated with current values.
3. Admin edits heading, body (Markdown), and optionally selects a profile image.
4. On submit, the system converts Markdown to HTML, persists both, and redirects with a success message.

### 4.2 Display About Page (Public)

1. Visitor navigates to `/about`.
2. System loads the singleton about content record.
3. If content exists, page renders heading, HTML body, and profile image.
4. If no content exists, a default empty state is shown.
5. Response includes `Cache-Control: public, max-age=60, stale-while-revalidate=600`.

## 5. Security Considerations

- All write operations require JWT authentication.
- Markdown body is converted to HTML and sanitized to prevent XSS.
- Profile image is served via the existing digital asset pipeline with format negotiation.
- Public read operations are anonymous and cached.

## 6. Open Questions

| # | Question | Resolution |
|---|----------|------------|
| 1 | Should there be version history for about content? | No — single mutable record with optimistic concurrency via Version field. |
| 2 | Should the about page support multiple sections? | No — single heading + body is sufficient for v1. |
| 3 | Should the profile image be required? | No — optional, with graceful fallback when absent. |
