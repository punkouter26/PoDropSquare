// main-minimal.bicep - Minimal infrastructure deployment (without App Service due to quota)
targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment (e.g., dev, prod)')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string = 'eastus'

// Tags that should be applied to all resources
var tags = {
  'azd-env-name': environmentName
  application: 'PoDropSquare'
  environment: environmentName
}

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Resource group name - simple and clean
var resourceGroupName = 'PoDropSquare'

// Create resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Deploy core resources (Storage + Monitoring only, no App Service)
module resources './resources-minimal.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    location: location
    tags: tags
    resourceToken: resourceToken
  }
}

// Outputs
output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output APPLICATIONINSIGHTS_CONNECTION_STRING string = resources.outputs.applicationInsightsConnectionString
output AZURE_STORAGE_CONNECTION_STRING string = resources.outputs.storageConnectionString
output LOG_ANALYTICS_WORKSPACE_ID string = resources.outputs.logAnalyticsWorkspaceId
output STORAGE_ACCOUNT_NAME string = resources.outputs.storageAccountName
