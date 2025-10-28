// resources.bicep - Core Azure resources for PoDropSquare
param location string
param tags object
param resourceToken string
param principalId string

// Storage Account for Azure Table Storage
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'stpds${resourceToken}'  // Shortened: stpds = storage-podropsquare
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'  // Cheapest option
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

// Log Analytics Workspace (required for Application Insights)
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-podropsquare-${resourceToken}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-podropsquare-${resourceToken}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    RetentionInDays: 30
  }
}

// Use existing App Service Plan from PoShared resource group
// PoShared in East US 2 - Free tier with 6 apps (can host up to 10)
resource existingAppServicePlan 'Microsoft.Web/serverfarms@2022-09-01' existing = {
  name: 'PoShared'
  scope: resourceGroup('PoShared')
}

// App Service (named after solution file)
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'PoDropSquare'
  location: 'eastus2'  // Must match the existing plan's location
  tags: union(tags, {
    'azd-service-name': 'api'
  })
  kind: 'app'
  properties: {
    serverFarmId: existingAppServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v9.0'
      alwaysOn: false  // Free tier doesn't support always on
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__AzureTableStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
      ]
    }
  }
}

// Grant the App Service managed identity access to storage if principalId provided
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId)) {
  name: guid(storageAccount.id, principalId, 'StorageTableDataContributor')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3') // Storage Table Data Contributor
    principalId: principalId
    principalType: 'User'  // Changed from ServicePrincipal since azd uses user identity
  }
}

// Outputs
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output storageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
output appServiceName string = appService.name
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output storageAccountName string = storageAccount.name
