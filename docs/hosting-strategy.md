# Hosting & Deployment Strategy

## Decision

**Minimal Azure** — single-region, cost-optimised hosting with two environments (staging and production). No CDN tier initially. Total estimated spend: **~$150/month**.

---

## Environments

| | Staging | Production |
|--|---------|-----------|
| Resource group | `rg-blog-staging` | `rg-blog-production` |
| App Service SKU | B1 (1 vCore, 1.75 GB) | B1 (1 vCore, 1.75 GB) |
| Azure SQL SKU | Basic (5 DTU, 2 GB) | Basic (5 DTU, 2 GB) |
| Blob Storage | Standard LRS | Standard LRS |
| Key Vault | Standard | Standard |
| Application Insights | Free tier (5 GB/month) | Free tier (5 GB/month) |
| Deployment slot | No | No |
| Auto-scale | No | No |
| Custom domain / TLS | No | Yes |
| Region | East US | East US |

> Both environments use B1 to keep costs flat. Upgrade to P1v3 when traffic warrants it (see [Scaling](#scaling)).

---

## Monthly Cost Estimate

| Resource | Qty | Unit | Est./month |
|----------|-----|------|-----------|
| App Service B1 Linux × 2 (staging + prod) | 2 | $13.14/each | **$26** |
| Azure SQL Basic × 2 | 2 | $4.90/each | **$10** |
| Blob Storage LRS ~10 GB | 2 | <$1/each | **$2** |
| Key Vault Standard | 2 | <$1/each | **$2** |
| Application Insights | 2 | Free <5 GB | **$0** |
| Bandwidth egress ~5 GB | — | $0.087/GB | **<$1** |
| **Total** | | | **~$40/month** |

> Staging can be stopped outside business hours to cut compute cost to ~$26/month total.

---

## Git Workflow

```
commit to master
      │
      ▼
  GitHub Actions
  ├── build & test (.NET)
  ├── publish artifact
  └── deploy → Staging App Service
```

**Staging** receives every commit to `master` automatically.

**Production** is deployed manually when the team is ready to release:

```bash
# Pull the published artifact from the last successful staging run and redeploy to prod
gh workflow run deploy.yml -f environment=production
```

Or via the GitHub Actions UI: **Actions → CI/CD → Run workflow → production**.

---

## Deployment Pipeline

The workflow lives at `.github/workflows/deploy.yml`.

### Triggers

| Event | Action |
|-------|--------|
| Push to `master` | Build → test → deploy to staging |
| Manual `workflow_dispatch` with `environment=production` | Deploy same artifact to production |

### Jobs

```
build           dotnet restore → build → test → publish → upload artifact
   │
   └─ deploy-staging     download artifact → az webapp deploy → /health check
```

### Required GitHub secrets

| Secret | How to obtain |
|--------|--------------|
| `AZURE_CREDENTIALS` | `az ad sp create-for-rbac --sdk-auth` (see below) |
| `STAGING_ADMIN_EMAIL` | Email of the seeded admin user |
| `STAGING_ADMIN_PASSWORD` | Plain-text password used in Playwright tests |

Create the service principal (one-time setup):

```bash
az ad sp create-for-rbac \
  --name "github-blog-deploy" \
  --role contributor \
  --scopes \
    /subscriptions/<sub-id>/resourceGroups/rg-blog-staging \
    /subscriptions/<sub-id>/resourceGroups/rg-blog-production \
  --sdk-auth
```

Paste the JSON output as the `AZURE_CREDENTIALS` secret in **GitHub → Settings → Secrets and variables → Actions**.

---

## Provisioning

Run once per environment to create all Azure resources:

```bash
# Install prerequisites
az extension add --name webapp

# Set secrets in your shell
export JWT_SECRET=$(openssl rand -base64 32)
export SQL_ADMIN_PASSWORD="<strong-password>"
export ADMIN_PASSWORD_HASH="<pbkdf2-hash>"   # see docs/developer-guide.md §7

# Provision staging
./infra/scripts/provision.sh staging

# Provision production
./infra/scripts/provision.sh production
```

The script creates the resource group and deploys `infra/main.bicep` with the correct parameter file for the environment.

---

## Rollback

Because there is no deployment slot on B1, rollback means redeploying the previous artifact:

```bash
# Find the last good run
gh run list --workflow=deploy.yml --status=success --limit=5

# Redeploy it
gh run rerun <run-id>
```

For a faster path, upgrade to P1v3 to gain a free deployment slot and enable zero-downtime swap (see `docs/deployment-strategy.md`).

---

## Scaling

Upgrade path when B1 becomes insufficient:

| Symptom | Action | Est. cost increase |
|---------|--------|--------------------|
| Memory > 80% | Upgrade to B2 (3.5 GB) | +$13/month |
| CPU spikes > 70% | Upgrade to P1v3 + auto-scale | +$43/month |
| SQL DTU consistently at 100% | Upgrade to S1 (20 DTU) | +$25/month |
| Need zero-downtime deploys | Upgrade to P1v3 (slot included) | +$43/month |

---

## Infrastructure Files

```
infra/
├── main.bicep                      # Orchestrates all modules
├── modules/
│   ├── appservice.bicep            # App Service Plan + Web App (+ slot in prod)
│   ├── sql.bicep                   # Azure SQL Server + Database
│   ├── storage.bicep               # Blob Storage + assets container
│   ├── keyvault.bicep              # Key Vault + secrets
│   ├── keyvault-access.bicep       # RBAC: managed identity → Key Vault
│   ├── storage-access.bicep        # RBAC: managed identity → Blob Storage
│   └── monitoring.bicep            # Log Analytics + Application Insights
├── parameters/
│   ├── staging.bicepparam
│   └── production.bicepparam
└── scripts/
    ├── provision.sh                # One-shot provisioning script
    └── rollback.sh                 # Manual rollback helper
```
