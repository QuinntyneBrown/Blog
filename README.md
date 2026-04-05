# Blog

An open-source blog platform built with .NET, focused on articles, SEO, web performance, and discoverability by search engines, AI agents, and crawlers.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/download)
[![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)]()

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Features](#features)
- [Getting Started](#getting-started)
- [API Reference](#api-reference)
- [Requirements & Design](#requirements--design)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

Blog is a narrowly-scoped, production-grade blogging platform built on **ASP.NET Core 8**. It prioritizes implementation quality over feature breadth — every decision optimizes for correctness, security, performance, and discoverability.

**Design goals:**

- Sub-200ms TTFB on article pages
- Core Web Vitals compliance (LCP < 2.5s, CLS < 0.1, INP < 200ms)
- OWASP Top 10 hardened out of the box
- WCAG 2.1 Level AA accessible
- Machine-readable via JSON-LD, RSS/Atom, sitemap, and `llms.txt`

---

## Architecture

The platform is structured around three concerns:

| Layer | Description |
|---|---|
| **Public Site** | Server-rendered Razor Pages, anonymous, SEO-first |
| **Back Office** | Authenticated admin UI for content and asset management |
| **API Layer** | Shared application services and REST endpoints |

```
Blog/
├── src/
│   ├── Blog.Api/            # ASP.NET Core host (Razor Pages + REST API)
│   ├── Blog.Domain/         # Domain model and business rules
│   └── Blog.Infrastructure/ # EF Core, persistence, external services
├── test/
│   ├── Blog.UnitTests/      # Unit tests
│   └── Blog.Testing/        # Shared test helpers and fixtures
├── docs/
│   ├── specs/               # L1/L2 requirements (ATDD)
│   └── detailed-designs/    # Architecture documents with PlantUML diagrams
├── designs/
│   ├── exports/             # Public site screen designs (PNG)
│   └── exports-admin/       # Back office screen designs (PNG)
└── Blog.sln
```

Detailed C4 and sequence diagrams for each subsystem are in [`docs/detailed-designs/`](docs/detailed-designs/).

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 8, Razor Pages |
| Application | MediatR, FluentValidation |
| Data | Entity Framework Core, SQL Server |
| Auth | JWT bearer tokens, PBKDF2-SHA256 password hashing |
| Frontend | Server-side rendered Razor Pages, < 50 KB JS (gzipped) |
| Assets | Object storage (production), local filesystem (development) |

---

## Features

<details>
<summary><strong>Public Site</strong></summary>

- Paginated article listing with responsive 3/2/1-column grid
- Article detail pages optimized for reading (~70ch line width)
- JSON-LD structured data (`Schema.org Article` / `Blog`)
- Open Graph and Twitter Card meta tags
- XML sitemap, RSS 2.0 feed, Atom feed, `robots.txt`, `llms.txt`
- Critical CSS inlining, Brotli compression, immutable asset caching
- Core Web Vitals compliant
- WCAG 2.1 Level AA accessible

</details>

<details>
<summary><strong>Back Office</strong></summary>

- Article CRUD with rich text editor and live slug generation
- Draft / Published workflow with publish confirmation
- Digital asset management with drag-and-drop upload
- Image optimization (WebP/AVIF, responsive `srcset`, lazy loading)
- Sidebar navigation (desktop) and hamburger menu (mobile)

</details>

<details>
<summary><strong>Security</strong></summary>

- OWASP Top 10 hardened
- Input validation and XSS sanitization at all API boundaries
- HTTPS-only with HSTS, CSP, and security headers
- Layered rate limiting: 10 req/min per IP and 5 req/15 min per email on auth endpoints; 60 req/min on write endpoints
- Strict CORS policy

</details>

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or SQL Server Express / LocalDB for local development)

### Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/QuinntyneBrown/Blog.git
cd Blog

# 2. Restore dependencies
dotnet restore

# 3. Apply database migrations
dotnet ef database update --project src/Blog.Infrastructure --startup-project src/Blog.Api

# 4. Run the application
dotnet run --project src/Blog.Api
```

The application will be available at `https://localhost:5001`.

### Running Tests

```bash
dotnet test
```

---

## API Reference

All write endpoints require a valid JWT bearer token obtained from `/api/auth/login`.

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/login` | — | Authenticate and receive a JWT |
| `POST` | `/api/auth/logout` | Required | Revoke the current session token |
| `GET` | `/api/articles` | — | List articles (paginated) |
| `GET` | `/api/articles/{id}` | — | Get article by ID |
| `POST` | `/api/articles` | Required | Create an article |
| `PUT` | `/api/articles/{id}` | Required | Update an article |
| `PATCH` | `/api/articles/{id}/publish` | Required | Publish or unpublish an article |
| `DELETE` | `/api/articles/{id}` | Required | Delete an article |
| `POST` | `/api/digital-assets` | Required | Upload an image asset |
| `GET` | `/api/digital-assets/{id}` | — | Get asset metadata |
| `GET` | `/health` | — | Liveness health check |
| `GET` | `/health/ready` | — | Readiness health check |
| `GET` | `/sitemap.xml` | — | XML sitemap |
| `GET` | `/feed.xml` | — | RSS 2.0 feed |
| `GET` | `/atom.xml` | — | Atom feed |

---

## Requirements & Design

Requirements are tracked using **Acceptance Test Driven Development (ATDD)**:

- **[L1 Requirements](docs/specs/L1.md)** — 16 high-level capabilities
- **[L2 Requirements](docs/specs/L2.md)** — 73 detailed requirements with 230+ acceptance criteria

Every acceptance test includes a trace comment back to its L2 requirement:

```csharp
// Acceptance Test
// Traces to: L2-001, L2-025
// Description: Verify article creation with input validation
```

UI designs are created in [Pencil](https://pencil.dev) using a SpaceX-inspired dark theme. The implementation targets 5 responsive breakpoints:

| Name | Threshold |
|---|---|
| XS | < 576px |
| SM | ≥ 576px |
| MD | ≥ 768px |
| LG | ≥ 992px |
| XL | ≥ 1200px |

Design exports are in [`designs/exports/`](designs/exports/) (public site) and [`designs/exports-admin/`](designs/exports-admin/) (back office).

---

## Contributing

We welcome contributions. Please read [CONTRIBUTING.md](CONTRIBUTING.md) for development workflow, branch naming, commit conventions, and pull request guidelines before submitting changes.

---

## License

This project is licensed under the **MIT License**. See [LICENSE](LICENSE) for details.
