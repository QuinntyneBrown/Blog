# Developer Guide

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Getting Started](#2-getting-started)
3. [Project Structure](#3-project-structure)
4. [Configuration](#4-configuration)
5. [Database Setup](#5-database-setup)
6. [Running the Application](#6-running-the-application)
7. [Authentication](#7-authentication)
8. [Architecture Overview](#8-architecture-overview)
9. [Testing](#9-testing)
10. [Deployment to Azure](#10-deployment-to-azure)

---

## 1. Prerequisites

Install the following tools before working on this project:

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| SQL Server | LocalDB / Express / Full | https://aka.ms/sqllocaldb |
| Node.js | 18+ (for E2E tests) | https://nodejs.org |
| Git | Any | https://git-scm.com |

**Optional:**
- Visual Studio 2022 or JetBrains Rider (IDE support)
- Azure CLI (for deployment): `winget install Microsoft.AzureCLI`
- Docker Desktop (for containerised deployment)

Verify your setup:

```bash
dotnet --version    # Should print 8.x.x
node --version      # Should print 18.x.x or higher
sqlcmd -?           # Should print usage if SQL Server tools are present
```

---

## 2. Getting Started

### Clone the repository

```bash
git clone https://github.com/QuinntyneBrown/Blog.git
cd Blog
```

### Restore dependencies

```bash
dotnet restore
```

### Install Playwright dependencies (E2E tests only)

```bash
cd test/Blog.Playwright
npm install
npx playwright install
cd ../..
```

### Verify the build

```bash
dotnet build
```

---

## 3. Project Structure

```
Blog/
├── src/
│   ├── Blog.Api/               # Web host: Razor Pages + REST API
│   ├── Blog.Domain/            # Domain entities and business rules (no dependencies)
│   └── Blog.Infrastructure/    # EF Core, repositories, storage, migrations
├── test/
│   ├── Blog.Api.Tests/         # Unit tests for API handlers and services
│   ├── Blog.Domain.Tests/      # Domain entity tests
│   ├── Blog.Infrastructure.Tests/
│   ├── Blog.Integration.Tests/ # In-process integration tests (WebApplicationFactory)
│   ├── Blog.Web.Tests/         # Razor Pages model tests
│   ├── Blog.Playwright/        # End-to-end browser tests (TypeScript)
│   └── Blog.Testing/           # Shared test fixtures and helpers
├── docs/                       # This file and all project documentation
└── Blog.sln
```

### Key directories inside `Blog.Api`

```
Blog.Api/
├── Features/           # CQRS vertical slices (Commands + Queries per feature)
│   ├── Articles/
│   ├── Auth/
│   ├── DigitalAssets/
│   └── ...
├── Middleware/         # Custom ASP.NET Core middleware
├── Pages/              # Razor Pages (public site + admin)
│   ├── Admin/          # Authenticated admin area
│   └── ...
├── Services/           # Application services (TokenService, SlugGenerator, etc.)
├── wwwroot/            # Static files; assets uploaded at runtime go to wwwroot/assets/
├── appsettings.json
└── Program.cs
```

---

## 4. Configuration

### appsettings.json (production defaults)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BlogDb;Trusted_Connection=True;"
  },
  "Jwt": {
    "Secret": "REPLACE-WITH-A-STRONG-SECRET-AT-LEAST-32-CHARS-LONG",
    "Issuer": "blog-api",
    "Audience": "blog-clients",
    "ExpirationMinutes": "60"
  },
  "Cors": {
    "AllowedOrigins": "https://localhost:5001"
  },
  "Site": {
    "SiteName": "My Blog",
    "SiteUrl": "https://example.com",
    "AuthorName": "Author Name",
    "PublisherName": "Publisher Name"
  },
  "RateLimiting": {
    "LoginPermitLimit": 10,
    "WritePermitLimit": 60
  },
  "Seed": {
    "AdminUser": {
      "Email": "admin@example.com",
      "DisplayName": "Admin",
      "PasswordHash": "<pbkdf2-hash>"
    }
  }
}
```

### appsettings.Development.json (local overrides)

The development override ships with a pre-hashed admin credential:

```
Email:    admin@blog.dev
Password: Admin1234!
```

The `Jwt:Secret` is set to a development-only value. **Never use this secret in production.**

### Environment variable overrides

Any `appsettings.json` key can be overridden via environment variables using `__` as the section separator:

```bash
ConnectionStrings__DefaultConnection="Server=...;Database=...;"
Jwt__Secret="my-production-secret"
```

This is the recommended approach for secrets in Azure (see [Section 10](#10-deployment-to-azure)).

---

## 5. Database Setup

The application uses **Entity Framework Core** with SQL Server. Migrations are applied automatically when the application starts.

### Local development (SQL Server LocalDB)

LocalDB is included with Visual Studio. No manual setup is required — the default connection string points to `(localdb)\mssqllocaldb` and the database is created on first run.

### Manual migration commands

If you need to manage migrations manually:

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project src/Blog.Infrastructure \
  --startup-project src/Blog.Api

# Apply pending migrations
dotnet ef database update \
  --project src/Blog.Infrastructure \
  --startup-project src/Blog.Api

# Roll back to a specific migration
dotnet ef database update <MigrationName> \
  --project src/Blog.Infrastructure \
  --startup-project src/Blog.Api
```

### Seed data

On startup the app seeds the following if the database is empty:

| Environment | What gets seeded |
|-------------|-----------------|
| All | Admin user from `Seed:AdminUser` in appsettings |
| Development | 3 sample articles (Hello World, ASP.NET Core guide, Clean Architecture) |

---

## 6. Running the Application

### Start the API and web frontend

```bash
dotnet run --project src/Blog.Api
```

The application starts on:

- **HTTPS:** `https://localhost:5001`
- **HTTP:** `http://localhost:5000` (redirects to HTTPS)

### Key URLs

| URL | Description |
|-----|-------------|
| `https://localhost:5001` | Public blog |
| `https://localhost:5001/admin` | Admin dashboard (requires login) |
| `https://localhost:5001/admin/login` | Login page |
| `https://localhost:5001/swagger` | OpenAPI / Swagger UI |
| `https://localhost:5001/health` | Health check endpoint |
| `https://localhost:5001/health/ready` | Readiness probe |

### Hot reload during development

```bash
dotnet watch --project src/Blog.Api
```

---

## 7. Authentication

### Admin login

1. Navigate to `https://localhost:5001/admin/login`
2. Enter credentials:
   - **Email:** `admin@blog.dev`
   - **Password:** `Admin1234!`
3. A JWT token is issued and stored in the session.

### API authentication

The REST API uses **JWT Bearer** tokens.

**Obtain a token:**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@blog.dev",
  "password": "Admin1234!"
}
```

**Response:**

```json
{
  "token": "eyJhbGci...",
  "expiresAt": "2026-04-05T13:00:00Z"
}
```

**Use the token:**

```http
GET /api/articles
Authorization: Bearer eyJhbGci...
```

### JWT configuration requirements

- `Jwt:Secret` must be **at least 32 characters** (256 bits). The application throws on startup if this is not met.
- Tokens expire after `Jwt:ExpirationMinutes` (default: 60).
- Algorithm: HS256.

### Rate limiting

| Policy | Limit | Applies to |
|--------|-------|-----------|
| Login | 10 req/min | Per IP address |
| Write endpoints | 60 req/min | Per authenticated user ID or IP |

Exceeded limits return `429 Too Many Requests` with a `Retry-After` header.

---

## 8. Architecture Overview

### Layered dependency flow

```
Blog.Api  →  Blog.Infrastructure  →  Blog.Domain
```

`Blog.Domain` has zero external dependencies. `Blog.Infrastructure` depends only on EF Core. `Blog.Api` wires everything together.

### CQRS with MediatR

Every feature is a vertical slice under `Features/{FeatureName}/`:

```
Features/Articles/
├── Commands/
│   ├── CreateArticle/
│   │   ├── CreateArticleCommand.cs
│   │   ├── CreateArticleCommandHandler.cs
│   │   └── CreateArticleCommandValidator.cs
│   └── ...
└── Queries/
    ├── GetArticles/
    └── GetArticleBySlug/
```

MediatR pipeline (in order):

1. **ValidationBehavior** — runs FluentValidation; throws `ValidationException` on failure
2. **LoggingBehavior** — logs request name, timing, and correlation ID

### Request lifecycle

```
HTTP Request
  → ExceptionHandlingMiddleware
  → CorrelationIdMiddleware
  → RequestLoggingMiddleware
  → SecurityHeadersMiddleware
  → JwtMiddleware
  → Controller / Razor Page
    → MediatR.Send(command)
      → ValidationBehavior
      → LoggingBehavior
      → Handler → Repository → DbContext
  → ResponseEnvelopeMiddleware
HTTP Response
```

### Response envelope

All API responses are wrapped:

```json
{
  "data": { ... },
  "errors": []
}
```

Errors follow RFC 7807 Problem Details.

### Storage

Asset storage is abstracted behind `IAssetStorage`:

| Implementation | Used when |
|----------------|-----------|
| `LocalFileAssetStorage` | Development (writes to `wwwroot/assets/`) |
| `BlobAssetStorage` | Production (Azure Blob Storage) |

Switch by changing the DI registration in `Program.cs` or via configuration.

---

## 9. Testing

### Unit and integration tests (.NET)

```bash
# Run all tests
dotnet test

# Run a specific project
dotnet test test/Blog.Api.Tests
dotnet test test/Blog.Integration.Tests

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

Test projects and their purpose:

| Project | Purpose |
|---------|---------|
| `Blog.Api.Tests` | Handlers, middleware, services |
| `Blog.Domain.Tests` | Entity behaviour |
| `Blog.Infrastructure.Tests` | Repository logic |
| `Blog.Integration.Tests` | Full API stack via `WebApplicationFactory` |
| `Blog.Web.Tests` | Razor Page models |

### End-to-end tests (Playwright)

```bash
cd test/Blog.Playwright
```

Create a `.env` file (copy from `.env.example`):

```
BASE_URL=https://localhost:5001
API_URL=https://localhost:5001
ADMIN_EMAIL=admin@blog.dev
ADMIN_PASSWORD=Admin1234!
```

```bash
# Run all tests headlessly
npm test

# Run with browser visible
npm run test:headed

# Debug a specific test
npm run test:debug

# View the HTML report
npm run test:report
```

Playwright tests cover Chromium, Firefox, and WebKit and include accessibility checks via `@axe-core/playwright`.

---

## 10. Deployment to Azure

### Overview

The recommended Azure topology:

```
Internet
  └─ Azure Front Door (CDN + WAF)
       └─ App Service (Blog.Api)
            ├─ Azure SQL Database
            ├─ Azure Blob Storage (digital assets)
            └─ Azure Key Vault (secrets)
```

### Step 1 — Provision Azure resources

```bash
# Login
az login

# Variables — adjust to your naming convention
RESOURCE_GROUP="rg-blog-prod"
LOCATION="eastus"
APP_NAME="blog-api-prod"
SQL_SERVER="sql-blog-prod"
SQL_DB="BlogDb"
STORAGE_ACCOUNT="stblogprod"
KEYVAULT="kv-blog-prod"
APP_SERVICE_PLAN="asp-blog-prod"

# Resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# App Service Plan (Linux, B1 or higher)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Web App (.NET 8)
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNET|8.0"

# Azure SQL
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user sqladmin \
  --admin-password "<strong-password>"

az sql db create \
  --name $SQL_DB \
  --server $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --service-objective S1

# Blob Storage
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

az storage container create \
  --name assets \
  --account-name $STORAGE_ACCOUNT \
  --public-access blob

# Key Vault
az keyvault create \
  --name $KEYVAULT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

### Step 2 — Store secrets in Key Vault

```bash
# JWT secret (generate a strong random value)
JWT_SECRET=$(openssl rand -base64 32)

az keyvault secret set --vault-name $KEYVAULT --name "Jwt--Secret"              --value "$JWT_SECRET"
az keyvault secret set --vault-name $KEYVAULT --name "ConnectionStrings--DefaultConnection" \
  --value "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User Id=sqladmin;Password=<strong-password>;Encrypt=True;"
```

### Step 3 — Grant the App Service access to Key Vault

```bash
# Enable system-assigned managed identity on the Web App
az webapp identity assign \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId --output tsv)

# Grant Key Vault Secrets User role
az keyvault set-policy \
  --name $KEYVAULT \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Step 4 — Configure App Service application settings

```bash
# Connection string (pulled from Key Vault via managed identity reference)
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "Jwt__Issuer=blog-api" \
    "Jwt__Audience=blog-clients" \
    "Jwt__ExpirationMinutes=60" \
    "Cors__AllowedOrigins=https://yourdomain.com" \
    "Site__SiteName=My Blog" \
    "Site__SiteUrl=https://yourdomain.com" \
    "Site__AuthorName=Your Name" \
    "AssetStorage__Provider=AzureBlob" \
    "AssetStorage__ContainerName=assets" \
    "AssetStorage__AccountName=$STORAGE_ACCOUNT"

# Reference Key Vault secrets directly
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "Jwt__Secret=@Microsoft.KeyVault(VaultName=$KEYVAULT;SecretName=Jwt--Secret)" \
    "ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(VaultName=$KEYVAULT;SecretName=ConnectionStrings--DefaultConnection)"
```

### Step 5 — Deploy the application

#### Option A: Publish via dotnet CLI

```bash
dotnet publish src/Blog.Api \
  --configuration Release \
  --output ./publish

az webapp deploy \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src-path ./publish \
  --type zip
```

#### Option B: GitHub Actions (recommended)

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [master]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore

      - name: Test
        run: dotnet test --no-restore

      - name: Publish
        run: dotnet publish src/Blog.Api -c Release -o ./publish

      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

Store the publish profile from the Azure Portal under **Deployment Center → Manage publish profile** as the `AZURE_WEBAPP_PUBLISH_PROFILE` secret in your GitHub repository.

### Step 6 — Configure the SQL firewall

```bash
# Allow Azure services (App Service)
az sql server firewall-rule create \
  --name AllowAzureServices \
  --server $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### Step 7 — Configure custom domain (optional)

```bash
# Add custom domain
az webapp config hostname add \
  --webapp-name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname yourdomain.com

# Create a free managed TLS certificate
az webapp config ssl create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname yourdomain.com

# Bind the certificate
az webapp config ssl bind \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --certificate-thumbprint <thumbprint> \
  --ssl-type SNI
```

### Step 8 — Health checks and monitoring

The app exposes two health check endpoints:

| Endpoint | Purpose | Use for |
|----------|---------|---------|
| `/health` | Database + disk space | Azure App Service health probe |
| `/health/ready` | Readiness check | Load balancer / container orchestration |

Configure the health probe in the Azure Portal under **App Service → Health check** and set the path to `/health`.

**Enable Application Insights:**

```bash
az monitor app-insights component create \
  --app blog-insights \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP

INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app blog-insights \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey --output tsv)

az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings "ApplicationInsights__InstrumentationKey=$INSTRUMENTATION_KEY"
```

Structured logs from Serilog are automatically forwarded to Application Insights via the `Serilog.Sinks.ApplicationInsights` sink already configured in `appsettings.json`.

### Production checklist

Before going live verify the following:

- [ ] `Jwt:Secret` is at least 32 characters and stored in Key Vault
- [ ] `ASPNETCORE_ENVIRONMENT` is set to `Production`
- [ ] HTTPS is enforced (App Service has HTTPS Only enabled)
- [ ] CORS `AllowedOrigins` lists only your production domain
- [ ] `Seed:AdminUser` credentials are changed from the development defaults
- [ ] SQL Server firewall allows only App Service (not `0.0.0.0/0` from your workstation)
- [ ] Application Insights is connected and receiving logs
- [ ] Health check probe is configured in App Service
- [ ] Rate limiting values are appropriate for your expected traffic

---

## Additional Resources

- `docs/specs/L1.md` — High-level capability requirements
- `docs/specs/L2.md` — Detailed acceptance criteria
- `docs/detailed-designs/` — Per-subsystem architecture documents
- `docs/architecture-decision-records/` — ADRs for all major technical decisions
- `docs/conformance.md` — WCAG 2.1 AA and OWASP Top 10 audit checklist
