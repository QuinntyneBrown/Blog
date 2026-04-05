@description('Environment name: staging or production')
@allowed(['staging', 'production'])
param environmentName string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Admin email address for the seeded admin user')
param adminEmail string

@description('PBKDF2 password hash for the seeded admin user')
@secure()
param adminPasswordHash string

@description('JWT signing secret (minimum 32 characters)')
@secure()
param jwtSecret string

@description('SQL Server administrator login')
param sqlAdminLogin string = 'sqladmin'

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

@description('Public-facing site URL (e.g. https://yourdomain.com)')
param siteUrl string

@description('Display name shown in the blog header')
param siteName string = 'Quinn Brown'

@description('Author name used in structured data and meta tags')
param authorName string = 'Quinn Brown'

@description('Allowed CORS origins (comma-separated)')
param corsAllowedOrigins string

// ──────────────────────────────────────────────
// Naming
// ──────────────────────────────────────────────
var prefix = 'blog-${environmentName}'
var storageAccountName = replace('st${environmentName}blog', '-', '')  // storage names: lowercase alphanumeric only

// ──────────────────────────────────────────────
// Modules
// ──────────────────────────────────────────────
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    prefix: prefix
    location: location
    environmentName: environmentName
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    prefix: prefix
    location: location
    environmentName: environmentName
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    storageAccountName: storageAccountName
    location: location
    environmentName: environmentName
  }
}

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    prefix: prefix
    location: location
    jwtSecret: jwtSecret
    sqlConnectionString: sql.outputs.connectionString
    adminEmail: adminEmail
    adminPasswordHash: adminPasswordHash
  }
}

module appservice 'modules/appservice.bicep' = {
  name: 'appservice'
  params: {
    prefix: prefix
    location: location
    environmentName: environmentName
    keyVaultName: keyvault.outputs.keyVaultName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    storageAccountName: storageAccountName
    siteUrl: siteUrl
    siteName: siteName
    authorName: authorName
    corsAllowedOrigins: corsAllowedOrigins
  }
  dependsOn: [keyvault, monitoring, storage]
}

// Grant App Service managed identity access to Key Vault
module keyvaultAccess 'modules/keyvault-access.bicep' = {
  name: 'keyvaultAccess'
  params: {
    keyVaultName: keyvault.outputs.keyVaultName
    principalId: appservice.outputs.principalId
  }
}

// Grant App Service managed identity access to Blob Storage
module storageAccess 'modules/storage-access.bicep' = {
  name: 'storageAccess'
  params: {
    storageAccountName: storageAccountName
    principalId: appservice.outputs.principalId
  }
}

// ──────────────────────────────────────────────
// Outputs
// ──────────────────────────────────────────────
output appServiceName string = appservice.outputs.appServiceName
output appServiceUrl string = appservice.outputs.appServiceUrl
output sqlServerFqdn string = sql.outputs.serverFqdn
output keyVaultName string = keyvault.outputs.keyVaultName
output storageAccountName string = storageAccountName
output appInsightsName string = monitoring.outputs.appInsightsName
