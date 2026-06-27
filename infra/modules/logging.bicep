@description('Base name prefix for all resources')
param prefix string

param location string

@description('Passed from main: the App Registration objectId created by the deploymentScript')
param logIngestionPrincipalObjectId string

// ── Log Analytics Workspace ───────────────────────────────────────────────────

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 31
  }
}

// ── Data Collection Endpoint ──────────────────────────────────────────────────

resource dce 'Microsoft.Insights/dataCollectionEndpoints@2022-06-01' = {
  name: '${prefix}-dce'
  location: location
  properties: {
    networkAcls: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// ── Data Collection Rule ──────────────────────────────────────────────────────
// Column schema matches Serilog.Sinks.AzureLogAnalytics v7 output:
// The sink sends 3 fields per event: TimeGenerated, Event (full JSON), Message (rendered).
// Individual structured properties (Level, UserId, etc.) are nested inside Event (dynamic).
// Query them in KQL via: extend level = tostring(Event.Level)

var logColumns = [
  { name: 'TimeGenerated', type: 'datetime' }
  { name: 'Event',         type: 'dynamic'  }
  { name: 'Message',       type: 'string'   }
]

resource dcr 'Microsoft.Insights/dataCollectionRules@2022-06-01' = {
  name: '${prefix}-dcr'
  location: location
  properties: {
    dataCollectionEndpointId: dce.id
    streamDeclarations: {
      'Custom-TinyHeroesApi': {
        columns: logColumns
      }
      'Custom-TinyHeroesApiIntegration': {
        columns: logColumns
      }
    }
    destinations: {
      logAnalytics: [
        {
          workspaceResourceId: workspace.id
          name: 'tinyheroes-workspace'
        }
      ]
    }
    dataFlows: [
      {
        streams: [ 'Custom-TinyHeroesApi' ]
        destinations: [ 'tinyheroes-workspace' ]
        outputStream: 'Custom-TinyHeroesApi_CL'
        transformKql: 'source'
      }
      {
        streams: [ 'Custom-TinyHeroesApiIntegration' ]
        destinations: [ 'tinyheroes-workspace' ]
        outputStream: 'Custom-TinyHeroesApiIntegration_CL'
        transformKql: 'source'
      }
    ]
  }
}

// ── RBAC: Monitoring Metrics Publisher on the DCR ────────────────────────────
// Allows the App Registration service principal to ingest logs via the DCE/DCR.

var monitoringMetricsPublisherId = '3913510d-42f4-4e42-8a64-420c390055eb'

resource dcrRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(dcr.id, logIngestionPrincipalObjectId, monitoringMetricsPublisherId)
  scope: dcr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringMetricsPublisherId)
    principalId: logIngestionPrincipalObjectId
    principalType: 'ServicePrincipal'
  }
}

// ── Log Analytics Reader RBAC on the workspace ───────────────────────────────
// Allows the same service principal to be used by Grafana Cloud as a read-only
// Azure Monitor data source (optional — can be granted later).

var logAnalyticsReaderId = '73c42c96-874c-492b-b04d-ab87d138a893'

resource workspaceReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(workspace.id, logIngestionPrincipalObjectId, logAnalyticsReaderId)
  scope: workspace
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', logAnalyticsReaderId)
    principalId: logIngestionPrincipalObjectId
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output workspaceId string = workspace.id
output dceEndpoint string = dce.properties.logsIngestion.endpoint
output dcrImmutableId string = dcr.properties.immutableId
output dcrId string = dcr.id
