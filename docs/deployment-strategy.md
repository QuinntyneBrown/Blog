# Deployment Strategy & Cost Breakdown

## Table of Contents

1. [Environment Overview](#1-environment-overview)
2. [Azure Resource Topology](#2-azure-resource-topology)
3. [Staging Environment](#3-staging-environment)
4. [Production Environment](#4-production-environment)
5. [Cost Breakdown](#5-cost-breakdown)
6. [Deployment Pipeline](#6-deployment-pipeline)
7. [Promotion Workflow (Staging → Production)](#7-promotion-workflow-staging--production)
8. [Rollback Strategy](#8-rollback-strategy)
9. [Scaling Considerations](#9-scaling-considerations)

---

## 1. Environment Overview

| Attribute | Staging | Production |
|-----------|---------|-----------|
| Purpose | Pre-release validation, QA, smoke tests | Live public-facing blog |
| Traffic | Internal team + automated tests only | Public internet |
| Data | Anonymised copy of production or synthetic seed data | Real content |
| Uptime SLA | Best-effort (may be stopped overnight) | 99.9% target |
| Deployment trigger | Every merge to `master` | Manual promotion after staging sign-off |
| Azure region | East US | East US (primary) |

---

## 2. Azure Resource Topology

Both environments share the same resource types but differ in SKU and redundancy configuration.

```
┌──────────────────────────────────────────────────┐
│  GitHub Actions CI/CD                            │
│  build → test → publish → deploy                 │
└──────────┬───────────────────┬───────────────────┘
           │                   │
           ▼                   ▼
  ┌─────────────────┐  ┌─────────────────────────┐
  │    STAGING      │  │      PRODUCTION          │
  │                 │  │                          │
  │  App Service    │  │  App Service             │
  │  (B1, Linux)    │  │  (P1v3, Linux)           │
  │                 │  │  + Deployment Slot       │
  │  Azure SQL      │  │  (staging slot)          │
  │  (Basic, 2 GB)  │  │                          │
  │                 │  │  Azure SQL               │
  │  Blob Storage   │  │  (S2, 250 GB)            │
  │  (LRS)          │  │                          │
  │                 │  │  Blob Storage            │
  │  Key Vault      │  │  (GRS)                   │
  │  (Standard)     │  │                          │
  │                 │  │  Key Vault               │
  │  App Insights   │  │  (Standard)              │
  │  (Basic)        │  │                          │
  └─────────────────┘  │  App Insights            │
                        │  (Standard)              │
                        │                          │
                        │  Azure Front Door        │
                        │  (Standard tier)         │
                        └─────────────────────────┘
```

---

## 3. Staging Environment

### Resource configuration

| Resource | SKU | Notes |
|----------|-----|-------|
| App Service Plan | B1 (1 vCore, 1.75 GB RAM) | Single instance, no auto-scale |
| Azure SQL Database | Basic (5 DTU, 2 GB) | Sufficient for test workloads |
| Blob Storage | Standard LRS | Single-region, no geo-redundancy needed |
| Key Vault | Standard | Secrets mirror production names |
| Application Insights | Pay-as-you-go (Basic sampling) | 1 GB/day free tier |
| App Service Health Check | Included | Path: `/health` |

### App Service configuration (staging)

```bash
RESOURCE_GROUP="rg-blog-staging"
APP_NAME="blog-api-staging"
APP_SERVICE_PLAN="asp-blog-staging"
SQL_SERVER="sql-blog-staging"
SQL_DB="BlogDb"
STORAGE_ACCOUNT="stblogstagingXXX"   # must be globally unique
KEYVAULT="kv-blog-staging"

az group create --name $RESOURCE_GROUP --location eastus

az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNET|8.0"

az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT=Staging \
    Jwt__Issuer=blog-api \
    Jwt__Audience=blog-clients \
    Jwt__ExpirationMinutes=60 \
    RateLimiting__LoginPermitLimit=10 \
    RateLimiting__WritePermitLimit=60
```

### Staging-specific appsettings

Create `appsettings.Staging.json` in `Blog.Api/`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  },
  "RateLimiting": {
    "LoginPermitLimit": 100,
    "WritePermitLimit": 1000
  },
  "Site": {
    "SiteUrl": "https://blog-api-staging.azurewebsites.net",
    "SiteName": "My Blog (Staging)"
  }
}
```

---

## 4. Production Environment

### Resource configuration

| Resource | SKU | Notes |
|----------|-----|-------|
| App Service Plan | P1v3 (2 vCores, 8 GB RAM) | Supports deployment slots + auto-scale |
| App Service Deployment Slot | `staging` slot (free with P1v3+) | Used for zero-downtime swap |
| Azure SQL Database | S2 (50 DTU, 250 GB) | Supports up to ~150 concurrent connections |
| Blob Storage | Standard GRS | Geo-redundant for asset durability |
| Key Vault | Standard | Managed identity access only |
| Application Insights | Pay-as-you-go | Adaptive sampling enabled |
| Azure Front Door | Standard | CDN, WAF, global anycast routing |
| App Service Health Check | Included | Path: `/health/ready` |

### App Service configuration (production)

```bash
RESOURCE_GROUP="rg-blog-prod"
APP_NAME="blog-api-prod"
APP_SERVICE_PLAN="asp-blog-prod"
SQL_SERVER="sql-blog-prod"
SQL_DB="BlogDb"
STORAGE_ACCOUNT="stblogprodXXX"
KEYVAULT="kv-blog-prod"

az group create --name $RESOURCE_GROUP --location eastus

az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku P1v3 \
  --is-linux

az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNET|8.0"

# Create deployment slot for zero-downtime releases
az webapp deployment slot create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --slot staging \
  --configuration-source $APP_NAME

az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    Jwt__Issuer=blog-api \
    Jwt__Audience=blog-clients \
    Jwt__ExpirationMinutes=60 \
    "Jwt__Secret=@Microsoft.KeyVault(VaultName=$KEYVAULT;SecretName=Jwt--Secret)" \
    "ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(VaultName=$KEYVAULT;SecretName=ConnectionStrings--DefaultConnection)"
```

### Auto-scale rule (production)

Scale out when CPU > 70% for 5 minutes; scale in when CPU < 30% for 10 minutes:

```bash
PLAN_ID=$(az appservice plan show \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --query id --output tsv)

az monitor autoscale create \
  --resource-group $RESOURCE_GROUP \
  --resource $PLAN_ID \
  --resource-type Microsoft.Web/serverfarms \
  --name autoscale-blog-prod \
  --min-count 1 \
  --max-count 3 \
  --count 1

az monitor autoscale rule create \
  --resource-group $RESOURCE_GROUP \
  --autoscale-name autoscale-blog-prod \
  --condition "CpuPercentage > 70 avg 5m" \
  --scale out 1

az monitor autoscale rule create \
  --resource-group $RESOURCE_GROUP \
  --autoscale-name autoscale-blog-prod \
  --condition "CpuPercentage < 30 avg 10m" \
  --scale in 1
```

---

## 5. Cost Breakdown

All prices are **USD, East US region, pay-as-you-go** as of April 2026. Actual billing depends on usage.

### Staging — estimated monthly cost

| Resource | SKU | Unit Price | Est. Monthly |
|----------|-----|-----------|-------------|
| App Service Plan | B1 Linux | $0.018/hr | **~$13** |
| Azure SQL Database | Basic 5 DTU | $0.0067/hr | **~$5** |
| Blob Storage (assets) | Standard LRS, ~5 GB | $0.018/GB | **<$1** |
| Blob Storage transactions | ~50k ops | $0.004/10k | **<$1** |
| Key Vault | Standard, <10k ops | $0.03/10k ops | **<$1** |
| Application Insights | <1 GB/day ingestion | First 5 GB/month free | **$0** |
| Bandwidth egress | ~1 GB/month | $0.087/GB | **<$1** |
| **Total** | | | **~$20–25/month** |

> **Tip:** Stop the staging App Service overnight and on weekends (no traffic) to cut the compute cost to ~$6/month.

```bash
# Stop staging outside business hours (e.g. via a scheduled Logic App or GitHub Action cron)
az webapp stop --name blog-api-staging --resource-group rg-blog-staging
az webapp start --name blog-api-staging --resource-group rg-blog-staging
```

### Production — estimated monthly cost

| Resource | SKU | Unit Price | Est. Monthly |
|----------|-----|-----------|-------------|
| App Service Plan | P1v3 Linux (1 instance) | $0.077/hr | **~$56** |
| App Service Plan | P1v3 Linux (2nd instance, auto-scale) | $0.077/hr × ~50% time | **~$28** |
| App Service Deployment Slot | Included in P1v3+ | — | **$0** |
| Azure SQL Database | S2 50 DTU | $0.15/hr | **~$110** |
| Blob Storage (assets) | Standard GRS, ~20 GB | $0.036/GB | **<$1** |
| Blob Storage transactions | ~500k ops | $0.004/10k | **<$1** |
| Key Vault | Standard | $0.03/10k ops | **<$1** |
| Application Insights | ~5 GB/month ingestion | $2.30/GB after 5 GB free | **~$0–10** |
| Azure Front Door | Standard tier | $35/month base + $0.008/GB | **~$40** |
| Bandwidth egress | ~10 GB/month | $0.087/GB | **~$1** |
| SQL Backup Storage | Included (7-day PITR) | — | **$0** |
| **Total** | | | **~$235–255/month** |

### Cost optimisation options

| Option | Saving | Trade-off |
|--------|--------|----------|
| Azure SQL Serverless (auto-pause) | ~40% on SQL | Cold-start latency on first request after pause |
| 1-year reserved App Service Plan | ~30% on compute | Upfront commitment |
| Front Door only for CDN (no WAF) | ~$15/month | Reduced DDoS / bot protection |
| Downgrade SQL to S1 (20 DTU) | ~$55/month | Lower concurrent connection cap |
| Use Azure Container Apps instead of App Service | Variable | Requires containerisation (no Dockerfile yet) |

### Total across both environments

| Scenario | Monthly |
|----------|---------|
| Minimal (staging stopped overnight, no Front Door) | **~$150** |
| Standard (as documented above) | **~$270** |
| With reserved instances (1-year) | **~$190** |

---

## 6. Deployment Pipeline

### GitHub Actions — full workflow

```
master branch push
       │
       ▼
  ┌─────────────┐
  │   CI Job    │  dotnet restore → build → test → publish
  └──────┬──────┘
         │ artifact: publish.zip
         ▼
  ┌─────────────────────┐
  │  Deploy to Staging  │  az webapp deploy → blog-api-staging
  └──────┬──────────────┘
         │
         ▼
  ┌─────────────────────┐
  │  Smoke Tests        │  Playwright headless against staging URL
  └──────┬──────────────┘
         │ pass
         ▼
  ┌─────────────────────┐
  │  Manual Approval    │  GitHub Environment protection rule
  └──────┬──────────────┘
         │ approved
         ▼
  ┌──────────────────────────────┐
  │  Deploy to Production Slot   │  az webapp deploy → blog-api-prod/staging slot
  └──────┬───────────────────────┘
         │
         ▼
  ┌──────────────────────────┐
  │  Health Check Validation  │  curl /health/ready on slot
  └──────┬───────────────────┘
         │ 200 OK
         ▼
  ┌──────────────────────────┐
  │  Slot Swap               │  staging slot ↔ production slot (zero downtime)
  └──────────────────────────┘
```

### `.github/workflows/deploy.yml`

```yaml
name: CI/CD

on:
  push:
    branches: [master]

env:
  DOTNET_VERSION: '8.0.x'
  STAGING_APP: blog-api-staging
  PROD_APP: blog-api-prod
  RESOURCE_GROUP_STAGING: rg-blog-staging
  RESOURCE_GROUP_PROD: rg-blog-prod

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release --logger "trx;LogFileName=results.trx"

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: "**/*.trx"

      - name: Publish
        run: dotnet publish src/Blog.Api -c Release -o ./publish --no-build

      - name: Upload artifact
        uses: actions/upload-artifact@v4
        with:
          name: blog-publish
          path: ./publish

  deploy-staging:
    name: Deploy to Staging
    needs: build-and-test
    runs-on: ubuntu-latest
    environment: staging
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: blog-publish
          path: ./publish

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy to Staging App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.STAGING_APP }}
          package: ./publish

      - name: Wait for app to start
        run: sleep 20

      - name: Health check
        run: |
          STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
            https://${{ env.STAGING_APP }}.azurewebsites.net/health)
          if [ "$STATUS" != "200" ]; then
            echo "Health check failed: HTTP $STATUS"
            exit 1
          fi

  smoke-tests:
    name: Playwright Smoke Tests
    needs: deploy-staging
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install Playwright
        working-directory: test/Blog.Playwright
        run: npm ci && npx playwright install --with-deps chromium

      - name: Run smoke tests
        working-directory: test/Blog.Playwright
        env:
          BASE_URL: https://${{ env.STAGING_APP }}.azurewebsites.net
          API_URL: https://${{ env.STAGING_APP }}.azurewebsites.net
          ADMIN_EMAIL: ${{ secrets.STAGING_ADMIN_EMAIL }}
          ADMIN_PASSWORD: ${{ secrets.STAGING_ADMIN_PASSWORD }}
        run: npx playwright test --project=chromium

      - name: Upload Playwright report
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: playwright-report
          path: test/Blog.Playwright/playwright-report/

  deploy-production:
    name: Deploy to Production
    needs: smoke-tests
    runs-on: ubuntu-latest
    environment: production          # requires manual approval in GitHub
    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: blog-publish
          path: ./publish

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy to Production staging slot
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ env.PROD_APP }}
          slot-name: staging
          package: ./publish

      - name: Wait for slot to warm up
        run: sleep 30

      - name: Health check on slot
        run: |
          STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
            https://${{ env.PROD_APP }}-staging.azurewebsites.net/health/ready)
          if [ "$STATUS" != "200" ]; then
            echo "Slot health check failed: HTTP $STATUS"
            exit 1
          fi

      - name: Swap slots (staging → production)
        run: |
          az webapp deployment slot swap \
            --name ${{ env.PROD_APP }} \
            --resource-group ${{ env.RESOURCE_GROUP_PROD }} \
            --slot staging \
            --target-slot production
```

### Required GitHub secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Service principal JSON (`az ad sp create-for-rbac`) |
| `STAGING_ADMIN_EMAIL` | Admin email for Playwright smoke tests |
| `STAGING_ADMIN_PASSWORD` | Admin password for Playwright smoke tests |

Create the service principal:

```bash
az ad sp create-for-rbac \
  --name "github-blog-deploy" \
  --role contributor \
  --scopes \
    /subscriptions/<sub-id>/resourceGroups/rg-blog-staging \
    /subscriptions/<sub-id>/resourceGroups/rg-blog-prod \
  --sdk-auth
```

Paste the JSON output as the `AZURE_CREDENTIALS` secret.

---

## 7. Promotion Workflow (Staging → Production)

### Zero-downtime slot swap

The production App Service Plan (P1v3) includes a free deployment slot named `staging`. New releases are deployed to this slot while the current production build continues to serve traffic. Once the slot passes its health check, a swap is performed — Azure routes traffic to the new version instantaneously with no connection drops.

```
Before swap:              After swap:
  /staging slot → new build   /production slot → new build
  /production slot → old build  /staging slot → old build (instant rollback available)
```

### Manual promotion steps (outside CI/CD)

```bash
# 1. Deploy to the staging slot manually
az webapp deploy \
  --name blog-api-prod \
  --resource-group rg-blog-prod \
  --slot staging \
  --src-path ./publish \
  --type zip

# 2. Verify the slot
curl https://blog-api-prod-staging.azurewebsites.net/health/ready

# 3. Swap
az webapp deployment slot swap \
  --name blog-api-prod \
  --resource-group rg-blog-prod \
  --slot staging \
  --target-slot production
```

### Environment protection rules (GitHub)

Configure these in **Settings → Environments** in your GitHub repository:

| Environment | Protection |
|-------------|-----------|
| `staging` | No restrictions — deploys automatically on merge to `master` |
| `production` | Required reviewers: 1 (team lead sign-off before swap) |

---

## 8. Rollback Strategy

### Immediate rollback via slot swap (< 30 seconds)

Because the previous production build remains in the `staging` slot after a swap, rolling back is a second swap:

```bash
az webapp deployment slot swap \
  --name blog-api-prod \
  --resource-group rg-blog-prod \
  --slot staging \
  --target-slot production
```

This is the **preferred rollback path** — it takes effect in under 30 seconds and does not require a new deployment.

### Database migration rollback

The application uses **forward-only migrations** (see ADR in `docs/architecture-decision-records/`). If a migration must be reversed:

1. Write a new migration that undoes the schema change.
2. Deploy it through the normal pipeline.
3. Never use `dotnet ef database update <previous-migration>` against a shared database.

### Application-level rollback (no slot available)

If the deployment slot is unavailable, redeploy the last known-good artifact:

```bash
# Find the last successful GitHub Actions run ID
gh run list --workflow=deploy.yml --status=success --limit=5

# Download its artifact and redeploy
gh run download <run-id> --name blog-publish --dir ./rollback
az webapp deploy \
  --name blog-api-prod \
  --resource-group rg-blog-prod \
  --src-path ./rollback \
  --type zip
```

---

## 9. Scaling Considerations

### When to scale up (vertical)

| Symptom | Action |
|---------|--------|
| Memory pressure (> 80% consistently) | Upgrade P1v3 → P2v3 (16 GB RAM) |
| High CPU during image processing | Add `ImageSharp` processing to a background job or upgrade vCores |
| SQL DTU at 100% | Upgrade S2 → S3 (100 DTU) or move to vCore model |

### When to scale out (horizontal)

The auto-scale rule in [Section 4](#4-production-environment) handles routine spikes. For planned high-traffic events (launches, social media mentions):

```bash
# Pre-scale to 2 instances before the event
az appservice plan update \
  --name asp-blog-prod \
  --resource-group rg-blog-prod \
  --number-of-workers 2
```

### Stateless design

The application is stateless with respect to compute — sessions use distributed memory cache and assets are stored in Blob Storage — so horizontal scaling requires no additional configuration.

### SQL connection pooling

EF Core uses `AddDbContextPool` (configured in `Program.cs`), which recycles `DbContext` instances and keeps SQL connections warm. The default pool size of 128 is appropriate for P1v3 with up to 3 instances.

If you scale beyond 3 instances, increase the SQL DTU tier before adding App Service instances to avoid exhausting the connection limit.
