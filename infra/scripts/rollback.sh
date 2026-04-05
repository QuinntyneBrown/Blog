#!/usr/bin/env bash
# rollback.sh — Swap production back to the previous build via slot swap.
#
# Usage:
#   ./infra/scripts/rollback.sh
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - AZURE_SUBSCRIPTION env var set (or current subscription is correct)

set -euo pipefail

PROD_APP="blog-production-api"
PROD_RG="rg-blog-production"

if [[ -n "${AZURE_SUBSCRIPTION:-}" ]]; then
  az account set --subscription "$AZURE_SUBSCRIPTION"
fi

echo "Rolling back production by swapping slots..."
az webapp deployment slot swap \
  --name "$PROD_APP" \
  --resource-group "$PROD_RG" \
  --slot staging \
  --target-slot production

echo ""
echo "Rollback complete. Previous build is now live in production."
echo "The failed build is now in the staging slot."
