param location string = 'northeurope'
param swaLocation string = 'westeurope'

@minLength(5)
param prefix string = 'tinyheroes'
param repositoryUrl string
param integrationBranch string = 'integration'

@secure()
param postgresAdminPassword string

@secure()
param jwtSecret string

@secure()
param repositoryToken string

// Logging credentials — these were created by main.bicep (prod deploy).
// Pass them in via --parameters flags; do not re-create the App Registration.
param logDceEndpoint string

@secure()
param logDcrImmutableId string

@secure()
param logTenantId string

@secure()
param logClientId string

@secure()
param logClientSecret string

// ── Reference existing shared resources ──────────────────────────────────────

resource existingAcr 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: '${prefix}acr'
}

resource existingPostgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' existing = {
  name: '${prefix}-pg'
}

// ── Integration database ──────────────────────────────────────────────────────

resource integrationDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: existingPostgres
  name: 'tinyheroes_integration'
}

// ── App Service (integration) ─────────────────────────────────────────────────

module appservice 'modules/appservice-integration.bicep' = {
  name: 'appservice-integration'
  params: {
    name: '${prefix}-api-integration'
    location: location
    existingAppServicePlanName: '${prefix}-api-plan'
    acrLoginServer: existingAcr.properties.loginServer
    acrUsername: existingAcr.name
    acrPassword: existingAcr.listCredentials().passwords[0].value
    postgresConnectionString: 'Host=${existingPostgres.properties.fullyQualifiedDomainName};Database=tinyheroes_integration;Username=tinyheroes;Password=${postgresAdminPassword};SslMode=Require'
    jwtSecret: jwtSecret
    allowedOrigin: 'https://integration.mytinyheroes.net'
    logDceEndpoint:    logDceEndpoint
    logDcrImmutableId: logDcrImmutableId
    logTenantId:       logTenantId
    logClientId:       logClientId
    logClientSecret:   logClientSecret
  }
  dependsOn: [ integrationDatabase ]
}

// ── Static Web App (integration) ──────────────────────────────────────────────

module swa 'modules/swa.bicep' = {
  name: 'swa-integration'
  params: {
    name: '${prefix}-frontend-integration'
    location: swaLocation
    repositoryUrl: repositoryUrl
    branch: integrationBranch
    repositoryToken: repositoryToken
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output appServiceName string = appservice.outputs.name
output appServiceHostname string = appservice.outputs.hostname
output swaHostname string = swa.outputs.hostname
