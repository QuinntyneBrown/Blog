# Blog Platform — Codebase Folder Structure

This document defines the folder structure for the Blog platform codebase. The solution follows a clean layered architecture with two deployable applications (API and Public Web) sharing a common domain and data access layer.

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
│   └── Blog.Integration.Tests/
│       ├── Blog.Integration.Tests.csproj
│       ├── ApiIntegrationTests.cs
│       └── WebIntegrationTests.cs
│
├── docs/
│   ├── folder-structure.md                   # This document
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
```

## Project Descriptions

| Project | Type | Purpose |
|---------|------|---------|
| **Blog.Domain** | Class Library | Domain entities (`Article`, `User`, `DigitalAsset`), repository interfaces, custom exceptions, and enums. No external dependencies. |
| **Blog.Infrastructure** | Class Library | EF Core `BlogDbContext`, entity configurations, repository implementations, unit of work, migrations, and file storage abstractions. |
| **Blog.Api** | ASP.NET Core Web API | Back-office REST API. Controllers, services, middleware (auth, security, rate limiting, logging), MediatR pipeline, validators, and DTOs. |
| **Blog.Web** | ASP.NET Core Razor Pages | Public-facing SSR site. Razor pages, view components, tag helpers, SEO generators (sitemap, feeds, JSON-LD), and performance middleware. |

## Key Conventions

- **Entities** live in `Blog.Domain/Entities/` — no framework dependencies
- **Repository interfaces** live in `Blog.Domain/Interfaces/` — implementations in `Blog.Infrastructure`
- **EF Core configurations** use `IEntityTypeConfiguration<T>` in `Blog.Infrastructure/Data/Configurations/`
- **Migrations** are auto-generated in `Blog.Infrastructure/Data/Migrations/` with timestamped names
- **DTOs** are grouped by feature domain (`Auth/`, `Articles/`, `DigitalAssets/`, `Common/`)
- **Middleware** is ordered intentionally in `Program.cs` — see the Security Hardening design for pipeline order
- **Static assets** in `Blog.Web/wwwroot/` use content-hashed filenames for immutable caching
- **Tests** mirror the `src/` project structure with one test project per source project plus an integration test project
