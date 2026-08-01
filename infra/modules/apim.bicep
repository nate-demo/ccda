// =============================================================================
// CCDA — Azure API Management
// Fronts the CCDA API with auth, rate-limiting, versioning, and monitoring.
// Imports the OpenAPI contract and wires App Insights diagnostics.
// Developer SKU is used for demos (no SLA, lowest cost).
// =============================================================================

@description('Azure region for API Management.')
param location string

@description('Resource name prefix, e.g. "ccda".')
param namePrefix string

@description('Publisher email shown in the developer portal / notifications.')
param publisherEmail string

@description('Publisher organization name.')
param publisherName string

@description('Base URL of the CCDA API backend that APIM routes to.')
param apiBackendUrl string

@description('Resource ID of the Application Insights component.')
param appInsightsId string

@description('Instrumentation key of the Application Insights component.')
@secure()
param appInsightsInstrumentationKey string

@description('Tags applied to every resource.')
param tags object = {}

resource apim 'Microsoft.ApiManagement/service@2023-05-01-preview' = {
  name: '${namePrefix}-apim'
  location: location
  tags: tags
  sku: {
    name: 'Developer'
    capacity: 1
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
  }
}

// Gateway-wide policy (CORS, correlation id, coarse rate limit).
resource globalPolicy 'Microsoft.ApiManagement/service/policies@2023-05-01-preview' = {
  parent: apim
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('../apim/policies/global.xml')
  }
}

// App Insights logger for request/dependency telemetry.
resource apimLogger 'Microsoft.ApiManagement/service/loggers@2023-05-01-preview' = {
  parent: apim
  name: 'appinsights'
  properties: {
    loggerType: 'applicationInsights'
    description: 'CCDA Application Insights logger'
    resourceId: appInsightsId
    credentials: {
      instrumentationKey: appInsightsInstrumentationKey
    }
  }
}

// Backend the API policy targets via <set-backend-service backend-id="ccda-api" />.
resource apiBackend 'Microsoft.ApiManagement/service/backends@2023-05-01-preview' = {
  parent: apim
  name: 'ccda-api'
  properties: {
    protocol: 'http'
    url: apiBackendUrl
    title: 'CCDA API backend'
  }
}

// Header-based versioning keeps the OpenAPI operation paths intact.
resource versionSet 'Microsoft.ApiManagement/service/apiVersionSets@2023-05-01-preview' = {
  parent: apim
  name: 'ccda-versions'
  properties: {
    displayName: 'CCDA Case Review API'
    versioningScheme: 'Header'
    versionHeaderName: 'Api-Version'
  }
}

// Import the API from the committed OpenAPI contract.
resource api 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  parent: apim
  name: 'ccda-api-v1'
  properties: {
    displayName: 'CCDA Case Review API'
    path: 'ccda'
    apiVersion: 'v1'
    apiVersionSetId: versionSet.id
    subscriptionRequired: true
    protocols: [
      'https'
    ]
    serviceUrl: apiBackendUrl
    format: 'openapi'
    value: loadTextContent('../apim/ccda-openapi.yaml')
  }
}

// API-level policy (auth template, per-subscription throttling + quota, backend).
resource apiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-05-01-preview' = {
  parent: api
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('../apim/policies/api.xml')
  }
  dependsOn: [
    apiBackend
  ]
}

// Per-API diagnostics into App Insights.
resource apiDiagnostics 'Microsoft.ApiManagement/service/apis/diagnostics@2023-05-01-preview' = {
  parent: api
  name: 'applicationinsights'
  properties: {
    alwaysLog: 'allErrors'
    loggerId: apimLogger.id
    sampling: {
      samplingType: 'fixed'
      percentage: 100
    }
    verbosity: 'information'
  }
}

// Product that requires a subscription and groups the API.
resource product 'Microsoft.ApiManagement/service/products@2023-05-01-preview' = {
  parent: apim
  name: 'ccda'
  properties: {
    displayName: 'CCDA Case Review'
    description: 'Citation-first legal case-review API for the Contoso County DA demo.'
    subscriptionRequired: true
    approvalRequired: false
    state: 'published'
  }
}

resource productApi 'Microsoft.ApiManagement/service/products/apis@2023-05-01-preview' = {
  parent: product
  name: api.name
}

@description('API Management gateway URL.')
output gatewayUrl string = apim.properties.gatewayUrl

@description('API Management resource name.')
output name string = apim.name

@description('Base URL for the CCDA API through APIM.')
output apiUrl string = '${apim.properties.gatewayUrl}/ccda'
