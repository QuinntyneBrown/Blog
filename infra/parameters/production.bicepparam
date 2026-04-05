using '../main.bicep'

param environmentName = 'production'
param location = 'eastus'

param siteUrl = 'https://yourdomain.com'
param siteName = 'Quinn Brown'
param authorName = 'Quinn Brown'
param corsAllowedOrigins = 'https://yourdomain.com'

param sqlAdminLogin = 'sqladmin'

param adminEmail = 'admin@yourdomain.com'

// ──────────────────────────────────────────────
// Secrets — do NOT commit real values.
// Supply at deploy time via:
//   az deployment group create ... \
//     --parameters @infra/parameters/production.bicepparam \
//                  jwtSecret=$JWT_SECRET \
//                  sqlAdminPassword=$SQL_ADMIN_PASSWORD \
//                  adminPasswordHash=$ADMIN_PASSWORD_HASH
// ──────────────────────────────────────────────
param jwtSecret = ''           // override at deploy time
param sqlAdminPassword = ''    // override at deploy time
param adminPasswordHash = ''   // override at deploy time
