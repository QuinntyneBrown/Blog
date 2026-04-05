@description('Resource name prefix')
param prefix string

@description('Azure region')
param location string

@description('Environment name')
@allowed(['staging', 'production'])
param environmentName string

@description('SQL Server administrator login')
param adminLogin string

@description('SQL Server administrator password')
@secure()
param adminPassword string

var isProduction = environmentName == 'production'
var serverName = 'sql-${prefix}'
var databaseName = 'BlogDb'

// ──────────────────────────────────────────────
// SQL Server
// ──────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: serverName
  location: location
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'  // restricted by firewall rules below
  }
}

// Allow Azure services (App Service) to connect
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  name: 'AllowAzureServices'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ──────────────────────────────────────────────
// Database
// ──────────────────────────────────────────────
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  name: databaseName
  parent: sqlServer
  location: location
  sku: {
    // Staging: Basic (5 DTU)  |  Production: S2 (50 DTU)
    name: isProduction ? 'S2' : 'Basic'
    tier: isProduction ? 'Standard' : 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: isProduction ? 268435456000 : 2147483648  // 250 GB : 2 GB
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: isProduction ? 'Geo' : 'Local'
  }
}

// ──────────────────────────────────────────────
// Diagnostic settings — send SQL metrics to Log Analytics (production)
// ──────────────────────────────────────────────

output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = databaseName
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Persist Security Info=False;User ID=${adminLogin};Password=${adminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
