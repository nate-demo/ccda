// =============================================================================
// CCDA — Azure AI Foundry (Azure OpenAI)
// Hosts the chat + embedding model deployments used by the AI orchestration
// pillar. RBAC-only (Entra ID); the workload identity gets "OpenAI User".
// =============================================================================

@description('Azure region for the Azure OpenAI account.')
param location string

@description('Resource name prefix, e.g. "ccda".')
param namePrefix string

@description('Chat completion model for summaries, briefings, and Q&A.')
param chatModelName string = 'gpt-4o-mini'

@description('Chat model version.')
param chatModelVersion string = '2024-07-18'

@description('Embedding model for vector search.')
param embeddingModelName string = 'text-embedding-3-small'

@description('Embedding model version.')
param embeddingModelVersion string = '1'

@description('Principal ID of the workload identity to grant inference access.')
param workloadPrincipalId string

@description('Resource ID of the Log Analytics workspace for diagnostics.')
param logAnalyticsId string

@description('Tags applied to every resource.')
param tags object = {}

// Cognitive Services OpenAI User — data-plane inference (no key needed).
var openAiUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'

resource foundry 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${namePrefix}-openai'
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    // Custom subdomain is required for Entra ID (AAD) token authentication.
    customSubDomainName: '${namePrefix}-openai'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundry
  name: chatModelName
  sku: {
    name: 'Standard'
    capacity: 20
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: foundry
  name: embeddingModelName
  sku: {
    name: 'Standard'
    capacity: 20
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: embeddingModelName
      version: embeddingModelVersion
    }
  }
  // Deployments on one account must be created serially.
  dependsOn: [
    chatDeployment
  ]
}

resource openAiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(foundry.id, workloadPrincipalId, openAiUserRoleId)
  scope: foundry
  properties: {
    principalId: workloadPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', openAiUserRoleId)
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'to-law'
  scope: foundry
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

@description('Azure OpenAI endpoint.')
output endpoint string = foundry.properties.endpoint

@description('Azure OpenAI account name.')
output name string = foundry.name

@description('Deployed chat model deployment name.')
output chatDeploymentName string = chatDeployment.name

@description('Deployed embedding model deployment name.')
output embeddingDeploymentName string = embeddingDeployment.name
