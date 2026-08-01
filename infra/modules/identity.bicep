// =============================================================================
// CCDA — user-assigned managed identity
// A single workload identity the API/Web use to reach AI Search and Foundry
// with RBAC (no keys in application configuration).
// =============================================================================

@description('Azure region for the identity.')
param location string

@description('Resource name prefix, e.g. "ccda".')
param namePrefix string

@description('Tags applied to every resource.')
param tags object = {}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${namePrefix}-id'
  location: location
  tags: tags
}

@description('Resource ID of the user-assigned managed identity.')
output id string = identity.id

@description('Client ID of the user-assigned managed identity.')
output clientId string = identity.properties.clientId

@description('Principal (object) ID used for role assignments.')
output principalId string = identity.properties.principalId

@description('Name of the user-assigned managed identity.')
output name string = identity.name
