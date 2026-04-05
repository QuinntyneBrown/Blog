using '../main.bicep'

param environmentName = 'staging'
param location = 'eastus'

param siteUrl = 'https://blog-staging-api.azurewebsites.net'
param siteName = 'Quinn Brown (Staging)'
param authorName = 'Quinn Brown'
param corsAllowedOrigins = 'https://blog-staging-api.azurewebsites.net'

param sqlAdminLogin = 'sqladmin'

param adminEmail = 'admin@blog.dev'

// ──────────────────────────────────────────────
// Secrets — do NOT commit real values.
// Supply at deploy time via:
//   az deployment group create ... \
//     --parameters @infra/parameters/staging.bicepparam \
//                  jwtSecret=$JWT_SECRET \
//                  sqlAdminPassword=$SQL_ADMIN_PASSWORD \
//                  adminPasswordHash=$ADMIN_PASSWORD_HASH
// ──────────────────────────────────────────────
param jwtSecret = ''           // override at deploy time
param sqlAdminPassword = ''    // override at deploy time
param adminPasswordHash = ''   // override at deploy time
