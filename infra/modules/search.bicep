// =============================================================================
// CCDA — Azure AI Search
// Hybrid (keyword + vector) + semantic search backing the case index.
// RBAC-only (local API keys disabled); the workload identity is granted the
// data-plane roles the API needs to create the index and read/write documents.
// =============================================================================

@description('Azure region for the search service.')
param location string

@description('Resource name prefix, e.g. "ccda".')
param namePrefix string

@description('Search SKU. "standard" supports semantic ranking and vectors.')
@allowed([
  'basic'
  'standard'
  'standard2'
])
param sku string = 'standard'

@description('Principal ID of the workload identity to grant data-plane access.')
param workloadPrincipalId string

@description('Resource ID of the Log Analytics workspace for diagnostics.')
param logAnalyticsId string

@description('Tags applied to every resource.')
param tags object = {}

// Built-in role definition IDs (data plane).
var searchIndexDataContributorRoleId = '8ebe5a00-799e-43f5-93ac-243d3dce84a7'
var searchServiceContributorRoleId = '7ca78c08-252a-4471-8644-bb5ff32d4ba0'

resource search 'Microsoft.Search/searchServices@2024-06-01-preview' = {
  name: '${namePrefix}-search'
  location: location
  tags: tags
  sku: {
    name: sku
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    semanticSearch: 'standard'
    // RBAC only — no local API keys in application configuration.
    disableLocalAuth: true
    publicNetworkAccess: 'enabled'
  }
}

resource dataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, workloadPrincipalId, searchIndexDataContributorRoleId)
  scope: search
  properties: {
    principalId: workloadPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataContributorRoleId)
  }
}

resource serviceContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, workloadPrincipalId, searchServiceContributorRoleId)
  scope: search
  properties: {
    principalId: workloadPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchServiceContributorRoleId)
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'to-law'
  scope: search
  properties: {
    workspaceId: logAnalyticsId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

@description('Search service endpoint, e.g. https://ccda-search.search.windows.net.')
output endpoint string = 'https://${search.name}.search.windows.net'

@description('Search service name.')
output name string = search.name
