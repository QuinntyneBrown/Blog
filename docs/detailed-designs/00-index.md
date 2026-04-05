# Detailed Designs — Index

| # | Feature | Status | Description |
|---|---------|--------|-------------|
| 01 | [Authentication & Authorization](01-authentication/README.md) | Implemented | JWT-based login, password security, token validation for back-office access |
| 02 | [Article Management](02-article-management/README.md) | Implemented | Back-office CRUD for articles with publish/unpublish workflow and reading time |
| 03 | [Public Article Display](03-public-article-display/README.md) | Implemented | SSR public site with responsive article listing and detail pages |
| 04 | [Digital Asset Management](04-digital-asset-management/README.md) | Implemented | Image upload, validation, storage, and optimized serving with format negotiation |
| 05 | [SEO & Discoverability](05-seo-and-discoverability/README.md) | Implemented | Semantic HTML, structured data, sitemaps, feeds, robots.txt, and llms.txt |
| 06 | [RESTful API](06-restful-api/README.md) | Implemented | API conventions, RFC 7807 errors, pagination, and endpoint catalog |
| 07 | [Web Performance](07-web-performance/README.md) | Implemented | SSR, critical CSS, caching, compression, and Core Web Vitals optimization |
| 08 | [Security Hardening](08-security-hardening/README.md) | Implemented | OWASP Top 10 mitigations, security headers, rate limiting, and CORS |
| 09 | [Observability](09-observability/README.md) | Implemented | Health checks, structured JSON logging, and correlation IDs |
| 10 | [Data Persistence](10-data-persistence/README.md) | Implemented | EF Core code-first, migrations, repository pattern, and database schema |
| 11 | [Search Infrastructure](11-search-infrastructure/README.md) | Implemented | SQL Server FTS index, SearchArticlesQuery, GetSearchSuggestionsQuery, and SearchHighlighter |
| 12 | [Search Input & Autocomplete](12-search-input/README.md) | Implemented | Global header search input, / shortcut, ARIA combobox dropdown, and search.js |
| 13 | [Search Results Page](13-search-results-page/README.md) | Implemented | /search Razor Page, result cards with mark highlighting, empty state, and pagination |
