# Blog

A personal blog platform built with .NET, narrowly focused on articles. Designed for 10/10 implementation quality, perfect SEO, extreme web performance, and maximum discoverability by search engines, AI agents, and bots.

## Architecture

The platform is built on ASP.NET Core 8 with Razor Pages for both public and back-office experiences plus JSON API endpoints where HTTP contracts are required:

- **Public Site** — Server-rendered Razor Pages, anonymous, optimized for sub-200ms TTFB and top-tier SEO/discoverability.
- **Back Office** — Authenticated Razor Pages administration UI for creating, editing, publishing articles and managing digital assets.
- **Application/API Layer** — Shared application services and REST endpoints for authenticated admin workflows, health checks, and machine-readable integrations.

```
Blog/
├── src/
│   └── Blog.Api/            # ASP.NET Core API (MediatR, EF Core)
├── test/
│   ├── Blog.UnitTests/
│   └── Blog.Testing/
├── docs/
│   ├── specs/               # L1/L2 requirements (ATDD)
│   └── detailed-designs/    # Architecture docs with PlantUML diagrams
├── designs/
│   ├── exports/             # Public site screen designs (PNG)
│   └── exports-admin/       # Back office screen designs (PNG)
└── Blog.sln
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Platform | ASP.NET Core 8, Razor Pages, MediatR, FluentValidation |
| Data | Entity Framework Core, PostgreSQL |
| Auth | JWT bearer tokens, PBKDF2-SHA256 password hashing |
| Public Site | Server-side rendered Razor Pages, minimal JS (<50 KB gzipped) |
| Back Office | Razor Pages admin with authenticated API calls |
| Assets | Object storage in production, local filesystem in development |

## Key Features

### Public Site
- Paginated article listing with 3/2/1 column responsive grid
- Article detail pages with optimal reading width (~70ch)
- JSON-LD structured data (Schema.org Article/Blog)
- Open Graph and Twitter Card meta tags
- XML sitemap, RSS/Atom feeds, robots.txt, llms.txt
- Critical CSS inlining, Brotli compression, immutable asset caching
- Core Web Vitals compliant (LCP < 2.5s, CLS < 0.1, INP < 200ms)
- WCAG 2.1 Level AA accessible

### Back Office
- Article CRUD with rich text editor and live slug generation
- Draft/Published workflow with publish confirmation
- Digital asset management with drag-and-drop upload
- Image optimization (WebP/AVIF, responsive srcset, lazy loading)
- Sidebar navigation on desktop, hamburger menu on mobile

### Security
- OWASP Top 10 hardened
- Input validation and XSS sanitization at API boundaries
- HTTPS-only with HSTS, CSP, and security headers
- Layered rate limiting on auth (10/min per IP and 5/15 min per normalized email) and write (60/min) endpoints
- Strict CORS policy

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later
- [PostgreSQL](https://www.postgresql.org/) 16+

### Setup

```bash
# Clone the repository
git clone https://github.com/QuinntyneBrown/Blog.git
cd Blog

# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project src/Blog.Api

# Run the application
dotnet run --project src/Blog.Api
```

The application will be available at `https://localhost:5001`.

### Running Tests

```bash
dotnet test
```

## Requirements

Requirements are tracked using Acceptance Test Driven Development (ATDD):

- **[L1 Requirements](docs/specs/L1.md)** — 12 high-level capabilities
- **[L2 Requirements](docs/specs/L2.md)** — 42 detailed requirements with 120+ acceptance criteria

Every acceptance test traces back to an L2 requirement:

```csharp
// Acceptance Test
// Traces to: L2-001, L2-025
// Description: Verify article creation with input validation
```

## Design

UI designs are created in [Pencil](https://pencil.dev) with the SpaceX-inspired dark theme. The implementation uses 5 responsive breakpoints: XS `< 576px`, SM `>= 576px`, MD `>= 768px`, LG `>= 992px`, and XL `>= 1200px`. The 375px and 1440px values used in the design files are reference canvas widths, not the CSS media-query thresholds.

Design exports are in `designs/exports/` (public site) and `designs/exports-admin/` (back office).

Detailed architecture documents with C4 and sequence diagrams are in `docs/detailed-designs/`.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Authenticate and receive JWT |
| POST | `/api/auth/logout` | Revoke the current admin token and end the session |
| GET | `/api/articles` | List articles (paginated) |
| GET | `/api/articles/{id}` | Get article by ID |
| POST | `/api/articles` | Create article |
| PUT | `/api/articles/{id}` | Update article |
| PATCH | `/api/articles/{id}/publish` | Publish or unpublish an article |
| DELETE | `/api/articles/{id}` | Delete article |
| POST | `/api/digital-assets` | Upload image |
| GET | `/api/digital-assets/{id}` | Get asset metadata |
| GET | `/health` | Public liveness-style health check |
| GET | `/health/ready` | Detailed readiness check |
| GET | `/sitemap.xml` | XML sitemap |
| GET | `/feed.xml` | RSS 2.0 feed |
| GET | `/atom.xml` | Atom feed |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
