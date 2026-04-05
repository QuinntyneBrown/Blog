@description('Storage account name (globally unique, lowercase alphanumeric, max 24 chars)')
param storageAccountName string

@description('Azure region')
param location string

@description('Environment name')
@allowed(['staging', 'production'])
param environmentName string

var isProduction = environmentName == 'production'

// ──────────────────────────────────────────────
// Storage Account
// ──────────────────────────────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    // Staging: LRS (single region)  |  Production: GRS (geo-redundant)
    name: isProduction ? 'Standard_GRS' : 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: true   // assets are publicly readable (images, attachments)
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowSharedKeyAccess: false   // managed identity only; no storage account keys in app config
  }
}

// ──────────────────────────────────────────────
// Blob Service
// ──────────────────────────────────────────────
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  name: 'default'
  parent: storageAccount
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: ['*']
          allowedMethods: ['GET', 'HEAD']
          allowedHeaders: ['*']
          exposedHeaders: ['ETag']
          maxAgeInSeconds: 86400
        }
      ]
    }
    deleteRetentionPolicy: {
      enabled: isProduction
      days: isProduction ? 7 : 1
    }
  }
}

// ──────────────────────────────────────────────
// Assets container (public read)
// ──────────────────────────────────────────────
resource assetsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: 'assets'
  parent: blobService
  properties: {
    publicAccess: 'Blob'
  }
}

output storageAccountId string = storageAccount.id
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
