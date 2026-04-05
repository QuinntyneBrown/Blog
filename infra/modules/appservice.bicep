@description('Resource name prefix')
param prefix string

@description('Azure region')
param location string

@description('Environment name')
@allowed(['staging', 'production'])
param environmentName string

@description('Key Vault name for secret references')
param keyVaultName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Storage account name for digital assets')
param storageAccountName string

@description('Canonical public URL of the site')
param siteUrl string

@description('Blog display name')
param siteName string

@description('Author name for structured data')
param authorName string

@description('Comma-separated CORS allowed origins')
param corsAllowedOrigins string

var isProduction = environmentName == 'production'

// ──────────────────────────────────────────────
// App Service Plan
// ──────────────────────────────────────────────
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: 'asp-${prefix}'
  location: location
  sku: {
    name: isProduction ? 'P1v3' : 'B1'
    tier: isProduction ? 'PremiumV3' : 'Basic'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true  // required for Linux
  }
}

// ──────────────────────────────────────────────
// App Service
// ──────────────────────────────────────────────
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: '${prefix}-api'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: isProduction
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health/ready'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: isProduction ? 'Production' : 'Staging'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'Jwt__Issuer'
          value: 'blog-api'
        }
        {
          name: 'Jwt__Audience'
          value: 'blog-clients'
        }
        {
          name: 'Jwt__ExpirationMinutes'
          value: '60'
        }
        {
          name: 'Jwt__Secret'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=Jwt--Secret)'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ConnectionStrings--DefaultConnection)'
        }
        {
          name: 'Seed__AdminUser__Email'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=Seed--AdminUser--Email)'
        }
        {
          name: 'Seed__AdminUser__PasswordHash'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=Seed--AdminUser--PasswordHash)'
        }
        {
          name: 'Cors__AllowedOrigins'
          value: corsAllowedOrigins
        }
        {
          name: 'Site__SiteUrl'
          value: siteUrl
        }
        {
          name: 'Site__SiteName'
          value: siteName
        }
        {
          name: 'Site__AuthorName'
          value: authorName
        }
        {
          name: 'Site__PublisherName'
          value: authorName
        }
        {
          name: 'AssetStorage__Provider'
          value: 'AzureBlob'
        }
        {
          name: 'AssetStorage__AccountName'
          value: storageAccountName
        }
        {
          name: 'AssetStorage__ContainerName'
          value: 'assets'
        }
        {
          name: 'RateLimiting__LoginPermitLimit'
          value: isProduction ? '10' : '100'
        }
        {
          name: 'RateLimiting__WritePermitLimit'
          value: isProduction ? '60' : '1000'
        }
      ]
    }
  }
}

// ──────────────────────────────────────────────
// Deployment slot (production only)
// ──────────────────────────────────────────────
resource stagingSlot 'Microsoft.Web/sites/slots@2023-01-01' = if (isProduction) {
  name: 'staging'
  parent: appService
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: false
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health/ready'
      appSettings: appService.properties.siteConfig.appSettings
    }
  }
}

// ──────────────────────────────────────────────
// Auto-scale (production only)
// ──────────────────────────────────────────────
resource autoScale 'Microsoft.Insights/autoscalesettings@2022-10-01' = if (isProduction) {
  name: 'autoscale-${prefix}'
  location: location
  properties: {
    enabled: true
    targetResourceUri: appServicePlan.id
    profiles: [
      {
        name: 'Default'
        capacity: {
          minimum: '1'
          maximum: '3'
          default: '1'
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT10M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
            }
          }
        ]
      }
    ]
  }
}

output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output principalId string = appService.identity.principalId
output stagingSlotPrincipalId string = isProduction ? stagingSlot.identity.principalId : ''
