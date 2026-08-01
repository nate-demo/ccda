// =============================================================================
// CCDA AI Case Review Platform — monitoring module
// Log Analytics workspace + Application Insights (workspace-based).
// All CCDA telemetry (API, Web, APIM) flows here.
// =============================================================================

@description('Azure region for the monitoring resources.')
param location string

@description('Resource name prefix, e.g. "ccda".')
param namePrefix string

@description('Tags applied to every resource.')
param tags object = {}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-law'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${namePrefix}-appi'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

@description('Resource ID of the Log Analytics workspace.')
output logAnalyticsId string = logAnalytics.id

@description('Resource ID of the Application Insights component.')
output appInsightsId string = appInsights.id

@description('Application Insights connection string (used by the API/Web OpenTelemetry exporter).')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Application Insights instrumentation key (used by the APIM logger).')
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
