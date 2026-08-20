// =============================================================================
// CCDA AI Case Review Platform — main deployment (resource-group scope)
// Orchestrates monitoring, identity, AI Search, Azure OpenAI (Foundry), and
// (optionally) API Management. RBAC-only data plane; no keys in app config.
//
// FOR DEMONSTRATION PURPOSES ONLY. Validate offline with:
//   az bicep build --file infra/main.bicep
// =============================================================================

targetScope = 'resourceGroup'

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Short name prefix for all resources (3-10 lowercase letters/digits).')
@minLength(3)
@maxLength(10)
param namePrefix string = 'ccda'

@description('Publisher email for API Management.')
param publisherEmail string = 'da-demo@contoso-county.example'

@description('Publisher organization name for API Management.')
param publisherName string = 'Contoso County District Attorney (Demo)'

@description('Chat completion model deployed to Azure OpenAI.')
param chatModelName string = 'gpt-4o-mini'

@description('Embedding model deployed to Azure OpenAI.')
param embeddingModelName string = 'text-embedding-3-small'

@description('AI Search SKU.')
@allowed([
  'basic'
  'standard'
  'standard2'
])
param searchSku string = 'standard'

@description('Deploy API Management. APIM provisioning is slow (~30-45 min); disable for quick infra demos.')
param deployApim bool = true

@description('Principal (object) ID of an externally-created workload identity — e.g. the user-assigned identity azd/Aspire provisions for the container apps. When empty (the default, used by the standalone `az deployment group create` path) this template creates and grants its own user-assigned identity.')
param workloadPrincipalId string = ''

@description('Base URL of the CCDA API backend that APIM routes to (App Service / Container App).')
param apiBackendUrl string = 'https://ccda-api.azurewebsites.net'

var tags = {
  application: 'ccda'
  purpose: 'demonstration'
  dataClassification: 'Demo Data'
  environment: 'demo'
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

// When an external workload principal is supplied (azd/Aspire path), reuse it and
// skip creating an identity here; otherwise create a self-contained one (standalone path).
var createIdentity = empty(workloadPrincipalId)

module identity 'modules/identity.bicep' = if (createIdentity) {
  name: 'identity'
  params: {
    location: location
    namePrefix: namePrefix
    tags: tags
  }
}

#disable-next-line BCP318
var effectivePrincipalId = createIdentity ? identity.outputs.principalId : workloadPrincipalId

module search 'modules/search.bicep' = {
  name: 'search'
  params: {
    location: location
    namePrefix: namePrefix
    sku: searchSku
    workloadPrincipalId: effectivePrincipalId
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    tags: tags
  }
}

module foundry 'modules/foundry.bicep' = {
  name: 'foundry'
  params: {
    location: location
    namePrefix: namePrefix
    chatModelName: chatModelName
    embeddingModelName: embeddingModelName
    workloadPrincipalId: effectivePrincipalId
    logAnalyticsId: monitoring.outputs.logAnalyticsId
    tags: tags
  }
}

module apim 'modules/apim.bicep' = if (deployApim) {
  name: 'apim'
  params: {
    location: location
    namePrefix: namePrefix
    publisherEmail: publisherEmail
    publisherName: publisherName
    apiBackendUrl: apiBackendUrl
    appInsightsId: monitoring.outputs.appInsightsId
    appInsightsInstrumentationKey: monitoring.outputs.appInsightsInstrumentationKey
    tags: tags
  }
}

@description('Application Insights connection string for the API/Web OTel exporter.')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString

@description('Workload managed identity client ID (empty when an external identity was supplied, e.g. the azd/Aspire path where AZURE_CLIENT_ID is injected by the platform).')
#disable-next-line BCP318
output workloadClientId string = createIdentity ? identity.outputs.clientId : ''

@description('Azure AI Search endpoint — set as Azure:Search:Endpoint.')
output searchEndpoint string = search.outputs.endpoint

@description('Azure OpenAI endpoint — set as Azure:Foundry:Endpoint.')
output foundryEndpoint string = foundry.outputs.endpoint

@description('Deployed chat model deployment name — set as Azure:Foundry:ChatDeployment.')
output chatDeploymentName string = foundry.outputs.chatDeploymentName

@description('Deployed embedding model deployment name — set as Azure:Foundry:EmbeddingDeployment.')
output embeddingDeploymentName string = foundry.outputs.embeddingDeploymentName

@description('API Management gateway URL (empty when APIM is not deployed).')
#disable-next-line BCP318
output apimGatewayUrl string = deployApim ? apim.outputs.gatewayUrl : ''

@description('CCDA API base URL through APIM (empty when APIM is not deployed).')
#disable-next-line BCP318
output apimApiUrl string = deployApim ? apim.outputs.apiUrl : ''
