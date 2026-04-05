@description('Resource name prefix')
param prefix string

@description('Azure region')
param location string

@description('JWT signing secret')
@secure()
param jwtSecret string

@description('Full SQL connection string')
@secure()
param sqlConnectionString string

@description('Admin user email for seed data')
param adminEmail string

@description('Admin user PBKDF2 password hash for seed data')
@secure()
param adminPasswordHash string

// ──────────────────────────────────────────────
// Key Vault
// ──────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${prefix}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true          // use RBAC rather than vault access policies
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

// ──────────────────────────────────────────────
// Secrets
// Key Vault secret names use '--' as the section separator
// because ':' and '__' are not allowed in secret names.
// ASP.NET Core config key: Jwt:Secret  →  secret name: Jwt--Secret
// ──────────────────────────────────────────────
resource secretJwt 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'Jwt--Secret'
  parent: keyVault
  properties: {
    value: jwtSecret
  }
}

resource secretConnStr 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'ConnectionStrings--DefaultConnection'
  parent: keyVault
  properties: {
    value: sqlConnectionString
  }
}

resource secretAdminEmail 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'Seed--AdminUser--Email'
  parent: keyVault
  properties: {
    value: adminEmail
  }
}

resource secretAdminHash 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'Seed--AdminUser--PasswordHash'
  parent: keyVault
  properties: {
    value: adminPasswordHash
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
