#!/usr/bin/env bash
# provision.sh — Create resource groups and deploy Bicep infrastructure.
#
# Usage:
#   ./infra/scripts/provision.sh staging
#   ./infra/scripts/provision.sh production
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Environment variables set (see below)
#
# Required environment variables:
#   JWT_SECRET          — >=32 character random string
#   SQL_ADMIN_PASSWORD  — Strong SQL administrator password
#   ADMIN_PASSWORD_HASH — PBKDF2 hash of the blog admin password
#                         (see docs/developer-guide.md for hash format)
#
# Optional environment variables:
#   AZURE_SUBSCRIPTION  — Subscription ID (defaults to current subscription)
#   LOCATION            — Azure region (defaults to eastus)

set -euo pipefail

# ──────────────────────────────────────────────
# Args
# ──────────────────────────────────────────────
ENVIRONMENT=${1:-}
if [[ -z "$ENVIRONMENT" || ( "$ENVIRONMENT" != "staging" && "$ENVIRONMENT" != "production" ) ]]; then
  echo "Usage: $0 <staging|production>"
  exit 1
fi

# ──────────────────────────────────────────────
# Config
# ──────────────────────────────────────────────
LOCATION=${LOCATION:-eastus}
RESOURCE_GROUP="rg-blog-${ENVIRONMENT}"
DEPLOYMENT_NAME="blog-${ENVIRONMENT}-$(date +%Y%m%d%H%M%S)"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

# ──────────────────────────────────────────────
# Validate required secrets
# ──────────────────────────────────────────────
: "${JWT_SECRET:?JWT_SECRET is required}"
: "${SQL_ADMIN_PASSWORD:?SQL_ADMIN_PASSWORD is required}"
: "${ADMIN_PASSWORD_HASH:?ADMIN_PASSWORD_HASH is required}"

if [[ ${#JWT_SECRET} -lt 32 ]]; then
  echo "ERROR: JWT_SECRET must be at least 32 characters"
  exit 1
fi

# ──────────────────────────────────────────────
# Set subscription (optional)
# ──────────────────────────────────────────────
if [[ -n "${AZURE_SUBSCRIPTION:-}" ]]; then
  echo "Setting subscription: $AZURE_SUBSCRIPTION"
  az account set --subscription "$AZURE_SUBSCRIPTION"
fi

SUBSCRIPTION=$(az account show --query id --output tsv)
echo "Using subscription: $SUBSCRIPTION"

# ──────────────────────────────────────────────
# Create resource group
# ──────────────────────────────────────────────
echo ""
echo "Creating resource group: $RESOURCE_GROUP ($LOCATION)"
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none

# ──────────────────────────────────────────────
# Deploy Bicep
# ──────────────────────────────────────────────
echo ""
echo "Deploying infrastructure for: $ENVIRONMENT"
echo "Deployment name: $DEPLOYMENT_NAME"

az deployment group create \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$INFRA_DIR/main.bicep" \
  --parameters "@$INFRA_DIR/parameters/${ENVIRONMENT}.bicepparam" \
  --parameters \
    jwtSecret="$JWT_SECRET" \
    sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
    adminPasswordHash="$ADMIN_PASSWORD_HASH" \
  --output table

# ──────────────────────────────────────────────
# Print outputs
# ──────────────────────────────────────────────
echo ""
echo "──────────────────────────────────────────"
echo " Deployment outputs"
echo "──────────────────────────────────────────"
az deployment group show \
  --name "$DEPLOYMENT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query properties.outputs \
  --output json

echo ""
echo "Provisioning complete."
echo ""
echo "Next steps:"
echo "  1. Configure GitHub secrets (AZURE_CREDENTIALS, etc.) — see docs/deployment-strategy.md"
echo "  2. Push to master to trigger the CI/CD pipeline"
echo "  3. For production: manually approve the deployment in GitHub Actions"
