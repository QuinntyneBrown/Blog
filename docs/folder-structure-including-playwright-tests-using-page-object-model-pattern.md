# Blog Platform — Codebase Folder Structure (with Playwright E2E Tests)

This document defines the complete folder structure for the Blog platform codebase, including a Playwright end-to-end test project using TypeScript and the Page Object Model pattern. The solution follows a clean layered architecture with two deployable applications (API and Public Web) sharing a common domain and data access layer.

```
Blog/
├── Blog.sln
│
├── src/
│   │
│   ├── Blog.Domain/                          # Domain entities and interfaces
│   │   ├── Blog.Domain.csproj
│   │   ├── Entities/
│   │   │   ├── Article.cs
│   │   │   ├── DigitalAsset.cs
│   │   │   └── User.cs
│   │   ├── Interfaces/
│   │   │   ├── IArticleRepository.cs
│   │   │   ├── IDigitalAssetRepository.cs
│   │   │   ├── IUserRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Exceptions/
│   │   │   ├── ConflictException.cs
│   │   │   ├── FileTooLargeException.cs
│   │   │   ├── NotFoundException.cs
│   │   │   ├── RateLimitExceededException.cs
│   │   │   └── ValidationException.cs
│   │   └── Enums/
│   │       ├── ImageFormat.cs
│   │       └── RateLimitKeyType.cs
│   │
│   ├── Blog.Infrastructure/                  # EF Core, file storage, external integrations
│   │   ├── Blog.Infrastructure.csproj
│   │   ├── Data/
│   │   │   ├── BlogDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── ArticleConfiguration.cs
│   │   │   │   ├── DigitalAssetConfiguration.cs
│   │   │   │   └── UserConfiguration.cs
│   │   │   ├── Migrations/
│   │   │   │   └── (EF Core auto-generated migrations)
│   │   │   ├── Repositories/
│   │   │   │   ├── ArticleRepository.cs
│   │   │   │   ├── DigitalAssetRepository.cs
│   │   │   │   └── UserRepository.cs
│   │   │   ├── UnitOfWork.cs
│   │   │   ├── MigrationRunner.cs
│   │   │   └── SeedData.cs
│   │   └── Storage/
│   │       ├── IAssetStorage.cs
│   │       ├── LocalFileAssetStorage.cs
│   │       └── BlobAssetStorage.cs
│   │
│   ├── Blog.Api/                             # Back-office API (REST)
│   │   ├── Blog.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Controllers/
│   │   │   ├── ApiController.cs              # Abstract base controller
│   │   │   ├── AuthController.cs
│   │   │   ├── ArticleController.cs
│   │   │   ├── DigitalAssetController.cs
│   │   │   └── HealthController.cs
│   │   ├── Services/
│   │   │   ├── Auth/
│   │   │   │   ├── AuthService.cs
│   │   │   │   ├── TokenService.cs
│   │   │   │   └── PasswordHasher.cs
│   │   │   ├── Articles/
│   │   │   │   ├── ArticleService.cs
│   │   │   │   ├── SlugGenerator.cs
│   │   │   │   └── ReadingTimeCalculator.cs
│   │   │   └── DigitalAssets/
│   │   │       ├── DigitalAssetService.cs
│   │   │       ├── FileValidator.cs
│   │   │       └── ImageProcessor.cs
│   │   ├── Middleware/
│   │   │   ├── JwtMiddleware.cs
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── ResponseEnvelopeMiddleware.cs
│   │   │   ├── SecurityHeadersMiddleware.cs
│   │   │   ├── HttpsRedirectionMiddleware.cs
│   │   │   ├── RateLimitingMiddleware.cs
│   │   │   ├── CorsMiddleware.cs
│   │   │   ├── AntiforgeryMiddleware.cs
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── LoggingBehavior.cs
│   │   ├── Validators/
│   │   │   ├── CreateArticleRequestValidator.cs
│   │   │   ├── UpdateArticleRequestValidator.cs
│   │   │   └── LoginRequestValidator.cs
│   │   ├── DTOs/
│   │   │   ├── Auth/
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   └── LoginResponse.cs
│   │   │   ├── Articles/
│   │   │   │   ├── ArticleDto.cs
│   │   │   │   ├── ArticleListDto.cs
│   │   │   │   ├── CreateArticleRequest.cs
│   │   │   │   └── UpdateArticleRequest.cs
│   │   │   ├── DigitalAssets/
│   │   │   │   ├── DigitalAssetDto.cs
│   │   │   │   ├── UploadResponse.cs
│   │   │   │   └── ImageTransformOptions.cs
│   │   │   └── Common/
│   │   │       ├── ApiResponse.cs
│   │   │       ├── PagedResponse.cs
│   │   │       └── PaginationParameters.cs
│   │   ├── Configuration/
│   │   │   ├── SecurityHeadersConfig.cs
│   │   │   ├── RateLimitPolicy.cs
│   │   │   ├── CorsConfig.cs
│   │   │   └── SiteConfiguration.cs
│   │   ├── Helpers/
│   │   │   ├── PaginationHelper.cs
│   │   │   ├── ProblemDetailsFactory.cs
│   │   │   └── HtmlSanitizer.cs
│   │   └── Observability/
│   │       └── LogSanitizer.cs
│   │
│   └── Blog.Web/                             # Public-facing SSR site (Razor Pages)
│       ├── Blog.Web.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Pages/
│       │   ├── _Layout.cshtml                # Shared layout (NavBar + Footer)
│       │   ├── _ViewImports.cshtml
│       │   ├── _ViewStart.cshtml
│       │   ├── Index.cshtml                  # Redirects to /articles
│       │   ├── Articles/
│       │   │   ├── Index.cshtml              # Article listing page
│       │   │   ├── Index.cshtml.cs           # ArticleListPage model
│       │   │   ├── Detail.cshtml             # Article detail page
│       │   │   └── Detail.cshtml.cs          # ArticleDetailPage model
│       │   └── Error.cshtml
│       ├── Components/
│       │   ├── NavDesktop.cshtml
│       │   ├── NavMobile.cshtml
│       │   ├── Footer.cshtml
│       │   ├── ArticleCard.cshtml
│       │   ├── Pagination.cshtml
│       │   ├── SkeletonCard.cshtml
│       │   └── EmptyState.cshtml
│       ├── Services/
│       │   ├── PublicArticleService.cs
│       │   └── PublicArticleController.cs    # Internal API for SSR data access
│       ├── TagHelpers/
│       │   ├── SeoMetaTagHelper.cs
│       │   ├── ImageTagHelper.cs
│       │   ├── ResourceHintTagHelper.cs
│       │   └── CriticalCssInliner.cs
│       ├── Seo/
│       │   ├── JsonLdGenerator.cs
│       │   ├── SitemapGenerator.cs
│       │   ├── FeedGenerator.cs
│       │   ├── RobotsTxtMiddleware.cs
│       │   ├── LlmsTxtMiddleware.cs
│       │   └── SlugRedirectMiddleware.cs
│       ├── Middleware/
│       │   ├── CompressionMiddleware.cs
│       │   ├── ResponseCachingMiddleware.cs
│       │   └── StaticFileMiddleware.cs
│       ├── DTOs/
│       │   ├── PublicArticleDto.cs
│       │   ├── ArticleListResponse.cs
│       │   ├── PaginationModel.cs
│       │   ├── SeoMetadata.cs
│       │   ├── JsonLdArticle.cs
│       │   ├── JsonLdPerson.cs
│       │   ├── JsonLdOrganization.cs
│       │   ├── SitemapEntry.cs
│       │   ├── FeedEntry.cs
│       │   ├── CacheProfile.cs
│       │   ├── ResourceHint.cs
│       │   └── PerformanceBudget.cs
│       ├── Configuration/
│       │   └── SiteConfiguration.cs
│       ├── Observability/
│       │   ├── HealthCheckService.cs
│       │   ├── DbHealthCheck.cs
│       │   ├── HealthCheckResponse.cs
│       │   ├── RequestLogEntry.cs
│       │   └── BusinessEvent.cs
│       ├── wwwroot/
│       │   ├── css/
│       │   │   ├── critical.css              # Inlined above-the-fold styles
│       │   │   └── site.css                  # Full stylesheet (async loaded)
│       │   ├── js/
│       │   │   └── site.js                   # Progressive enhancement only
│       │   ├── fonts/
│       │   │   └── (web fonts)
│       │   ├── favicon.ico
│       │   └── images/
│       │       └── (static site images)
│       └── uploads/                          # Local digital asset storage
│           └── (uploaded files)
│
├── test/
│   ├── Blog.Domain.Tests/
│   │   ├── Blog.Domain.Tests.csproj
│   │   └── Entities/
│   │       ├── ArticleTests.cs
│   │       ├── UserTests.cs
│   │       └── DigitalAssetTests.cs
│   ├── Blog.Infrastructure.Tests/
│   │   ├── Blog.Infrastructure.Tests.csproj
│   │   └── Repositories/
│   │       ├── ArticleRepositoryTests.cs
│   │       ├── UserRepositoryTests.cs
│   │       └── DigitalAssetRepositoryTests.cs
│   ├── Blog.Api.Tests/
│   │   ├── Blog.Api.Tests.csproj
│   │   ├── Controllers/
│   │   │   ├── AuthControllerTests.cs
│   │   │   ├── ArticleControllerTests.cs
│   │   │   ├── DigitalAssetControllerTests.cs
│   │   │   └── HealthControllerTests.cs
│   │   ├── Services/
│   │   │   ├── AuthServiceTests.cs
│   │   │   ├── ArticleServiceTests.cs
│   │   │   ├── SlugGeneratorTests.cs
│   │   │   ├── ReadingTimeCalculatorTests.cs
│   │   │   ├── FileValidatorTests.cs
│   │   │   └── ImageProcessorTests.cs
│   │   └── Middleware/
│   │       ├── RateLimitingMiddlewareTests.cs
│   │       ├── SecurityHeadersMiddlewareTests.cs
│   │       └── JwtMiddlewareTests.cs
│   ├── Blog.Web.Tests/
│   │   ├── Blog.Web.Tests.csproj
│   │   ├── Pages/
│   │   │   ├── ArticleListPageTests.cs
│   │   │   └── ArticleDetailPageTests.cs
│   │   ├── Seo/
│   │   │   ├── SitemapGeneratorTests.cs
│   │   │   ├── FeedGeneratorTests.cs
│   │   │   └── JsonLdGeneratorTests.cs
│   │   └── TagHelpers/
│   │       ├── SeoMetaTagHelperTests.cs
│   │       └── ImageTagHelperTests.cs
│   ├── Blog.Integration.Tests/
│   │   ├── Blog.Integration.Tests.csproj
│   │   ├── ApiIntegrationTests.cs
│   │   └── WebIntegrationTests.cs
│   │
│   └── Blog.Playwright/                     # ========== PLAYWRIGHT E2E TESTS ==========
│       ├── package.json
│       ├── package-lock.json
│       ├── tsconfig.json
│       ├── playwright.config.ts
│       ├── .env                              # Base URL overrides (not committed)
│       ├── .env.example                      # Template for environment variables
│       │
│       ├── fixtures/                         # Shared test fixtures and setup
│       │   ├── base.fixture.ts               # Extended test with all page objects auto-injected
│       │   ├── auth.fixture.ts               # Authenticated session fixture (login + storageState)
│       │   └── test-data.ts                  # Factory functions for test articles, users, assets
│       │
│       ├── helpers/                          # Utility functions shared across tests
│       │   ├── api-client.ts                 # Typed REST client for API setup/teardown
│       │   ├── wait-helpers.ts               # Custom wait conditions (toast visible, nav loaded)
│       │   └── viewport.ts                   # Viewport presets (XS, SM, MD, LG, XL)
│       │
│       ├── page-objects/                     # Page Object Model classes
│       │   │
│       │   ├── back-office/                  # Back-office (admin) page objects
│       │   │   ├── login.page.ts             # Login page — email, password, submit, error msg
│       │   │   ├── article-list.page.ts      # Articles list — table rows, search, new button, badges
│       │   │   ├── article-editor.page.ts    # Article editor — title, body, abstract, sidebar, publish
│       │   │   ├── digital-asset-modal.page.ts  # Asset upload modal — dropzone, preview, confirm
│       │   │   └── components/
│       │   │       ├── sidebar.component.ts  # Sidebar nav — nav items, active state, brand
│       │   │       ├── top-bar.component.ts  # Top bar — heading, action buttons, avatar
│       │   │       ├── toast.component.ts    # Toast notifications — success, error, warning, info
│       │   │       ├── modal.component.ts    # Generic modal — header, body, actions, close
│       │   │       ├── table-row.component.ts   # Article table row — title, status badge, date, actions
│       │   │       └── article-card.component.ts # Mobile article card (SM/XS breakpoints)
│       │   │
│       │   └── public/                       # Public-facing site page objects
│       │       ├── article-list.page.ts      # Article listing — card grid, pagination, empty state
│       │       ├── article-detail.page.ts    # Article detail — title, body, meta, featured image
│       │       ├── not-found.page.ts         # 404 error page
│       │       └── components/
│       │           ├── nav-desktop.component.ts  # Desktop nav bar — logo, links, RSS link
│       │           ├── nav-mobile.component.ts   # Mobile nav — hamburger, slide-out menu
│       │           ├── footer.component.ts       # Footer — links, copyright
│       │           ├── article-card.component.ts  # Article card — image, title, abstract, meta
│       │           └── pagination.component.ts    # Pagination — page numbers, prev/next
│       │
│       ├── tests/                            # Test specs organized by feature
│       │   │
│       │   ├── auth/                         # Authentication flows
│       │   │   ├── login.spec.ts             # Valid login, invalid credentials, empty fields, error display
│       │   │   ├── logout.spec.ts            # Logout clears session, redirects to login
│       │   │   ├── session-expiry.spec.ts    # Expired token redirects to login
│       │   │   └── protected-routes.spec.ts  # Unauthenticated access redirects to login
│       │   │
│       │   ├── article-management/           # Back-office article CRUD
│       │   │   ├── create-article.spec.ts    # Create with all fields, validation errors, slug generation
│       │   │   ├── edit-article.spec.ts      # Edit title/body/abstract, slug regeneration
│       │   │   ├── publish-article.spec.ts   # Publish draft, unpublish, verify status badge change
│       │   │   ├── delete-article.spec.ts    # Delete with confirmation modal, verify removal from list
│       │   │   ├── article-list.spec.ts      # List rendering, sorting, pagination, search
│       │   │   └── article-validation.spec.ts # Missing title, empty body, duplicate slug (409)
│       │   │
│       │   ├── digital-assets/               # Image upload and management
│       │   │   ├── upload-image.spec.ts      # Upload via dropzone, preview, success toast
│       │   │   ├── upload-validation.spec.ts # Invalid file type, oversized file (>10MB), error toast
│       │   │   ├── featured-image.spec.ts    # Set featured image in editor sidebar, remove image
│       │   │   └── image-serving.spec.ts     # Verify optimized image delivery, srcset, lazy loading
│       │   │
│       │   ├── public-site/                  # Public article display
│       │   │   ├── article-listing.spec.ts   # Published articles visible, drafts hidden, pagination
│       │   │   ├── article-detail.spec.ts    # Full article render, reading time, featured image
│       │   │   ├── empty-state.spec.ts       # No published articles shows empty state message
│       │   │   ├── not-found.spec.ts         # Non-existent slug, unpublished article slug → 404
│       │   │   └── slug-redirect.spec.ts     # Mixed-case slug 301 redirects to lowercase
│       │   │
│       │   ├── responsive/                   # Responsive layout tests across viewports
│       │   │   ├── nav-responsive.spec.ts    # Desktop nav at >=768px, hamburger at <768px, touch targets
│       │   │   ├── article-list-grid.spec.ts # 3-col XL, 2-col MD/LG, 1-col XS/SM, no horizontal scroll
│       │   │   ├── article-detail-layout.spec.ts  # Body max-width ~70ch at XL, full-width+padding at XS
│       │   │   ├── back-office-responsive.spec.ts # Sidebar at XL/LG, hamburger at MD/SM/XS, card layout at SM/XS
│       │   │   └── footer-responsive.spec.ts # Footer layout adapts across breakpoints
│       │   │
│       │   ├── seo/                          # SEO and discoverability
│       │   │   ├── meta-tags.spec.ts         # title, description, canonical on article + listing pages
│       │   │   ├── open-graph.spec.ts        # og:title, og:description, og:image, og:url, og:type
│       │   │   ├── twitter-cards.spec.ts     # twitter:card, twitter:title, twitter:description, twitter:image
│       │   │   ├── structured-data.spec.ts   # JSON-LD Article on detail, Blog on listing
│       │   │   ├── semantic-html.spec.ts     # article, header, main, nav, h1 uniqueness, heading hierarchy
│       │   │   ├── sitemap.spec.ts           # /sitemap.xml contains published articles, excludes unpublished
│       │   │   ├── feeds.spec.ts             # /feed.xml valid RSS 2.0, /atom.xml valid Atom
│       │   │   ├── robots-txt.spec.ts        # /robots.txt allows public, disallows /api/, has Sitemap
│       │   │   └── llms-txt.spec.ts          # /llms.txt present with site summary
│       │   │
│       │   ├── security/                     # Security hardening verification
│       │   │   ├── security-headers.spec.ts  # HSTS, CSP, X-Content-Type-Options, X-Frame-Options
│       │   │   ├── https-redirect.spec.ts    # HTTP requests 301 redirect to HTTPS
│       │   │   ├── xss-prevention.spec.ts    # Script tags in article body are sanitized on render
│       │   │   └── rate-limiting.spec.ts     # Login endpoint returns 429 after threshold
│       │   │
│       │   ├── performance/                  # Web performance verification
│       │   │   ├── cache-headers.spec.ts     # Static assets: immutable cache, HTML: short-lived + ETag
│       │   │   ├── compression.spec.ts       # Brotli/gzip content-encoding on text responses
│       │   │   ├── image-optimization.spec.ts # WebP/AVIF content negotiation, srcset, lazy loading
│       │   │   └── js-budget.spec.ts         # Total JS bundle under 50KB gzipped
│       │   │
│       │   ├── accessibility/                # WCAG 2.1 AA compliance
│       │   │   ├── axe-audit.spec.ts         # axe-core scan on listing + detail pages, zero violations
│       │   │   ├── keyboard-navigation.spec.ts  # Tab through all interactive elements
│       │   │   ├── image-alt-text.spec.ts    # All img elements have non-empty alt attributes
│       │   │   └── color-contrast.spec.ts    # Text contrast ratios meet 4.5:1 / 3:1 thresholds
│       │   │
│       │   ├── observability/                # Health and diagnostics
│       │   │   └── health-check.spec.ts      # /health returns 200 with status: healthy
│       │   │
│       │   └── api/                          # API contract tests via Playwright request context
│       │       ├── articles-api.spec.ts      # GET/POST/PUT/DELETE /api/articles, status codes, RFC 7807
│       │       ├── auth-api.spec.ts          # POST /api/auth/login, token format, 401 on invalid
│       │       ├── digital-assets-api.spec.ts # POST upload, GET metadata, file type validation
│       │       ├── pagination-api.spec.ts    # Page/pageSize params, total count, max cap at 100
│       │       └── error-responses-api.spec.ts  # RFC 7807 Problem Details format on 400/404/409/429
│       │
│       ├── global-setup.ts                   # Seed database, start servers, create auth storageState
│       ├── global-teardown.ts                # Clean up test data, stop servers
│       └── .gitignore                        # node_modules, test-results, playwright-report
│
├── docs/
│   ├── folder-structure.md
│   ├── folder-structure-including-playwright-tests-using-page-object-model-pattern.md  # This document
│   ├── specs/
│   │   ├── L1.md                             # High-level requirements
│   │   └── L2.md                             # Detailed requirements
│   ├── detailed-designs/
│   │   ├── 00-index.md
│   │   ├── 01-authentication/
│   │   ├── 02-article-management/
│   │   ├── 03-public-article-display/
│   │   ├── 04-digital-asset-management/
│   │   ├── 05-seo-and-discoverability/
│   │   ├── 06-restful-api/
│   │   ├── 07-web-performance/
│   │   ├── 08-security-hardening/
│   │   ├── 09-observability/
│   │   └── 10-data-persistence/
│   ├── ui-design-back-office.pen
│   └── ui-design-public-facing.pen
│
├── designs/                                  # Additional design assets
├── README.md
├── .gitignore
├── .editorconfig
└── Directory.Build.props                     # Shared MSBuild properties
```

## Project Dependency Graph

```
Blog.Domain          (no dependencies)
    ↑
Blog.Infrastructure  (depends on Blog.Domain)
    ↑
Blog.Api             (depends on Blog.Infrastructure, Blog.Domain)
Blog.Web             (depends on Blog.Infrastructure, Blog.Domain)

Blog.Playwright      (standalone TypeScript — hits Blog.Api + Blog.Web over HTTP)
```

## Project Descriptions

| Project | Type | Purpose |
|---------|------|---------|
| **Blog.Domain** | Class Library (.NET) | Domain entities (`Article`, `User`, `DigitalAsset`), repository interfaces, custom exceptions, and enums. No external dependencies. |
| **Blog.Infrastructure** | Class Library (.NET) | EF Core `BlogDbContext`, entity configurations, repository implementations, unit of work, migrations, and file storage abstractions. |
| **Blog.Api** | ASP.NET Core Web API | Back-office REST API. Controllers, services, middleware (auth, security, rate limiting, logging), MediatR pipeline, validators, and DTOs. |
| **Blog.Web** | ASP.NET Core Razor Pages | Public-facing SSR site. Razor pages, view components, tag helpers, SEO generators (sitemap, feeds, JSON-LD), and performance middleware. |
| **Blog.Playwright** | Playwright + TypeScript | End-to-end tests using Page Object Model pattern. Covers all major flows across both the back-office and public-facing sites. |

---

## Playwright Project Details

### Configuration (`playwright.config.ts`)

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html'], ['json', { outputFile: 'test-results/results.json' }]],
  globalSetup: './global-setup.ts',
  globalTeardown: './global-teardown.ts',
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    // Back-office tests (authenticated)
    {
      name: 'back-office-desktop',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: process.env.API_BASE_URL || 'http://localhost:5001',
        storageState: '.auth/admin.json',
      },
      testMatch: ['auth/**', 'article-management/**', 'digital-assets/**'],
    },
    // Public site — Desktop
    {
      name: 'public-desktop',
      use: {
        ...devices['Desktop Chrome'],
        baseURL: process.env.WEB_BASE_URL || 'http://localhost:5000',
      },
      testMatch: ['public-site/**', 'seo/**', 'security/**', 'performance/**', 'accessibility/**', 'observability/**'],
    },
    // Public site — Tablet
    {
      name: 'public-tablet',
      use: {
        ...devices['iPad (gen 7)'],
        baseURL: process.env.WEB_BASE_URL || 'http://localhost:5000',
      },
      testMatch: ['responsive/**'],
    },
    // Public site — Mobile
    {
      name: 'public-mobile',
      use: {
        ...devices['iPhone 13'],
        baseURL: process.env.WEB_BASE_URL || 'http://localhost:5000',
      },
      testMatch: ['responsive/**'],
    },
    // Back-office — Responsive
    {
      name: 'back-office-mobile',
      use: {
        ...devices['iPhone 13'],
        baseURL: process.env.API_BASE_URL || 'http://localhost:5001',
        storageState: '.auth/admin.json',
      },
      testMatch: ['responsive/back-office-responsive.spec.ts'],
    },
    // API contract tests (no browser)
    {
      name: 'api',
      use: {
        baseURL: process.env.API_BASE_URL || 'http://localhost:5001',
      },
      testMatch: ['api/**'],
    },
  ],
});
```

### Page Object Model Pattern

Each page object encapsulates locators and actions for a single page or reusable component. Tests never reference raw selectors — they interact through page object methods.

**Example: `page-objects/back-office/login.page.ts`**

```typescript
import { type Page, type Locator } from '@playwright/test';

export class LoginPage {
  readonly page: Page;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;
  readonly brandLogo: Locator;

  constructor(page: Page) {
    this.page = page;
    this.emailInput = page.getByLabel('Email');
    this.passwordInput = page.getByLabel('Password');
    this.submitButton = page.getByRole('button', { name: /sign in/i });
    this.errorMessage = page.getByRole('alert');
    this.brandLogo = page.getByText('QB');
  }

  async goto() {
    await this.page.goto('/login');
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }

  async getErrorText(): Promise<string> {
    return await this.errorMessage.innerText();
  }
}
```

**Example: `page-objects/back-office/article-list.page.ts`**

```typescript
import { type Page, type Locator } from '@playwright/test';
import { TableRowComponent } from './components/table-row.component';

export class ArticleListPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly newArticleButton: Locator;
  readonly searchInput: Locator;
  readonly tableRows: Locator;
  readonly emptyState: Locator;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole('heading', { name: 'Articles' });
    this.newArticleButton = page.getByRole('link', { name: /new/i });
    this.searchInput = page.getByPlaceholder(/search/i);
    this.tableRows = page.locator('[data-testid="article-row"]');
    this.emptyState = page.getByText(/no articles/i);
  }

  async goto() {
    await this.page.goto('/articles');
  }

  async getRowCount(): Promise<number> {
    return await this.tableRows.count();
  }

  getRow(index: number): TableRowComponent {
    return new TableRowComponent(this.tableRows.nth(index));
  }

  async clickNewArticle() {
    await this.newArticleButton.click();
  }
}
```

**Example: `page-objects/public/article-list.page.ts`**

```typescript
import { type Page, type Locator } from '@playwright/test';
import { ArticleCardComponent } from './components/article-card.component';
import { PaginationComponent } from './components/pagination.component';

export class PublicArticleListPage {
  readonly page: Page;
  readonly heading: Locator;
  readonly articleCards: Locator;
  readonly emptyState: Locator;
  readonly pagination: PaginationComponent;

  constructor(page: Page) {
    this.page = page;
    this.heading = page.getByRole('heading', { level: 1 });
    this.articleCards = page.locator('[data-testid="article-card"]');
    this.emptyState = page.getByTestId('empty-state');
    this.pagination = new PaginationComponent(page.getByRole('navigation', { name: /pagination/i }));
  }

  async goto(page = 1) {
    const url = page === 1 ? '/articles' : `/articles?page=${page}`;
    await this.page.goto(url);
  }

  async getCardCount(): Promise<number> {
    return await this.articleCards.count();
  }

  getCard(index: number): ArticleCardComponent {
    return new ArticleCardComponent(this.articleCards.nth(index));
  }
}
```

### Fixture Pattern (`fixtures/base.fixture.ts`)

```typescript
import { test as base } from '@playwright/test';
import { LoginPage } from '../page-objects/back-office/login.page';
import { ArticleListPage } from '../page-objects/back-office/article-list.page';
import { ArticleEditorPage } from '../page-objects/back-office/article-editor.page';
import { PublicArticleListPage } from '../page-objects/public/article-list.page';
import { PublicArticleDetailPage } from '../page-objects/public/article-detail.page';

type BlogFixtures = {
  loginPage: LoginPage;
  articleListPage: ArticleListPage;
  articleEditorPage: ArticleEditorPage;
  publicListPage: PublicArticleListPage;
  publicDetailPage: PublicArticleDetailPage;
};

export const test = base.extend<BlogFixtures>({
  loginPage: async ({ page }, use) => { await use(new LoginPage(page)); },
  articleListPage: async ({ page }, use) => { await use(new ArticleListPage(page)); },
  articleEditorPage: async ({ page }, use) => { await use(new ArticleEditorPage(page)); },
  publicListPage: async ({ page }, use) => { await use(new PublicArticleListPage(page)); },
  publicDetailPage: async ({ page }, use) => { await use(new PublicArticleDetailPage(page)); },
});

export { expect } from '@playwright/test';
```

### Test Coverage by Feature

| Feature | Test File | Flows Covered |
|---------|-----------|---------------|
| **Authentication** | `auth/login.spec.ts` | Valid login, invalid credentials, empty field validation, error message display |
| | `auth/logout.spec.ts` | Logout clears session, redirects to login |
| | `auth/session-expiry.spec.ts` | Expired JWT redirects to login page |
| | `auth/protected-routes.spec.ts` | Unauthenticated access to /articles editor → login redirect |
| **Article CRUD** | `article-management/create-article.spec.ts` | Create with title/body/abstract, slug auto-generation, success toast |
| | `article-management/edit-article.spec.ts` | Update fields, slug regeneration on title change |
| | `article-management/publish-article.spec.ts` | Publish draft → Published badge, unpublish → Draft badge |
| | `article-management/delete-article.spec.ts` | Delete via confirmation modal, removal from list |
| | `article-management/article-list.spec.ts` | Table rendering, pagination, row count |
| | `article-management/article-validation.spec.ts` | Missing title (400), duplicate slug (409) |
| **Digital Assets** | `digital-assets/upload-image.spec.ts` | Drag-and-drop upload, preview, success toast |
| | `digital-assets/upload-validation.spec.ts` | Reject non-image, reject >10MB |
| | `digital-assets/featured-image.spec.ts` | Set/remove featured image in article editor |
| | `digital-assets/image-serving.spec.ts` | WebP negotiation, srcset, lazy loading attributes |
| **Public Site** | `public-site/article-listing.spec.ts` | Published articles shown, drafts hidden, order by date desc |
| | `public-site/article-detail.spec.ts` | Title, body, reading time, featured image rendering |
| | `public-site/empty-state.spec.ts` | Empty state message when no articles published |
| | `public-site/not-found.spec.ts` | 404 for non-existent slug, 404 for unpublished article |
| | `public-site/slug-redirect.spec.ts` | Mixed-case slug → 301 → lowercase |
| **Responsive** | `responsive/nav-responsive.spec.ts` | Desktop nav at MD+, hamburger at SM/XS, 44px touch targets |
| | `responsive/article-list-grid.spec.ts` | 3-col XL, 2-col MD/LG, 1-col XS, no horizontal scroll |
| | `responsive/article-detail-layout.spec.ts` | ~70ch body width at XL, full-width+padding at XS |
| | `responsive/back-office-responsive.spec.ts` | Sidebar at XL/LG, hamburger at MD-, cards at SM/XS |
| | `responsive/footer-responsive.spec.ts` | Footer stacks vertically on small viewports |
| **SEO** | `seo/meta-tags.spec.ts` | `<title>`, `<meta description>`, `<link canonical>` |
| | `seo/open-graph.spec.ts` | All 6 `og:` tags present with correct values |
| | `seo/twitter-cards.spec.ts` | All 4 `twitter:` tags present |
| | `seo/structured-data.spec.ts` | JSON-LD `Article` on detail, `Blog` on listing |
| | `seo/semantic-html.spec.ts` | `<article>`, `<main>`, unique `<h1>`, heading hierarchy |
| | `seo/sitemap.spec.ts` | Valid XML, published articles present, unpublished excluded |
| | `seo/feeds.spec.ts` | `/feed.xml` valid RSS 2.0, `/atom.xml` valid Atom |
| | `seo/robots-txt.spec.ts` | Allow /, Disallow /api/, Sitemap directive |
| | `seo/llms-txt.spec.ts` | `/llms.txt` returns text with site summary |
| **Security** | `security/security-headers.spec.ts` | HSTS, CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy |
| | `security/https-redirect.spec.ts` | HTTP → 301 → HTTPS |
| | `security/xss-prevention.spec.ts` | Script tags in article body stripped/escaped |
| | `security/rate-limiting.spec.ts` | 429 after 10 rapid login attempts |
| **Performance** | `performance/cache-headers.spec.ts` | Static: immutable, HTML: short-lived + stale-while-revalidate |
| | `performance/compression.spec.ts` | Brotli/gzip Content-Encoding on responses |
| | `performance/image-optimization.spec.ts` | WebP/AVIF negotiation, srcset, lazy attributes |
| | `performance/js-budget.spec.ts` | Total JS < 50KB gzipped |
| **Accessibility** | `accessibility/axe-audit.spec.ts` | axe-core scan — zero critical/serious violations |
| | `accessibility/keyboard-navigation.spec.ts` | Tab through all interactive elements |
| | `accessibility/image-alt-text.spec.ts` | All `<img>` have non-empty `alt` |
| | `accessibility/color-contrast.spec.ts` | 4.5:1 body text, 3:1 large text |
| **Observability** | `observability/health-check.spec.ts` | GET /health → 200 + `{ status: "healthy" }` |
| **API Contracts** | `api/articles-api.spec.ts` | Full CRUD lifecycle, correct status codes |
| | `api/auth-api.spec.ts` | Login, token format, 401 on invalid |
| | `api/digital-assets-api.spec.ts` | Upload, metadata retrieval, type validation |
| | `api/pagination-api.spec.ts` | Page/pageSize params, totalCount, max 100 cap |
| | `api/error-responses-api.spec.ts` | RFC 7807 Problem Details on 400/404/409/429 |

### Key Conventions

- **Page objects** never contain assertions — they expose locators and actions; assertions live in test specs
- **Component objects** represent reusable UI fragments (sidebar, toast, card) and are composed into page objects
- **Fixtures** auto-instantiate page objects so tests receive them as parameters — no manual construction
- **`api-client.ts`** provides typed methods for test data setup/teardown (create article, upload image) via the REST API, avoiding UI interaction for preconditions
- **`global-setup.ts`** seeds the database, starts both servers, and performs a login to save `storageState` for authenticated test projects
- **Responsive tests** run against multiple Playwright projects (Desktop Chrome, iPad, iPhone) defined in `playwright.config.ts`
- **API contract tests** use `request` context (no browser) for lightweight validation of status codes, response shapes, and error formats
- **Accessibility tests** use `@axe-core/playwright` for automated WCAG 2.1 AA auditing
