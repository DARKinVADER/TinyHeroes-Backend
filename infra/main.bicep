param location string = 'northeurope'
param swaLocation string = 'westeurope'

@minLength(5)
param prefix string = 'tinyheroes'
param repositoryUrl string

@secure()
param postgresAdminPassword string

@secure()
param jwtSecret string

@secure()
param repositoryToken string

@secure()
param huggingFaceApiKey string

// ── App Registration for log ingestion ───────────────────────────────────────
// Bicep cannot create Entra App Registrations natively (they live in Microsoft Graph,
// not ARM). A deploymentScript runs az CLI to create the registration once and outputs
// the credentials. The script is idempotent: if the app already exists it reuses it.

resource logAppRegistrationScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: '${prefix}-log-app-reg'
  location: location
  kind: 'AzureCLI'
  properties: {
    azCliVersion: '2.60.0'
    retentionInterval: 'P1D'
    scriptContent: '''
      set -e
      APP_NAME="tinyheroes-log-ingestion"

      # Reuse existing app if it exists
      APP_ID=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv 2>/dev/null)

      if [ -z "$APP_ID" ]; then
        APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
      fi

      # Create service principal if missing
      SP_EXISTS=$(az ad sp show --id "$APP_ID" --query id -o tsv 2>/dev/null || true)
      if [ -z "$SP_EXISTS" ]; then
        az ad sp create --id "$APP_ID" > /dev/null
      fi

      SP_OBJECT_ID=$(az ad sp show --id "$APP_ID" --query id -o tsv)
      TENANT_ID=$(az account show --query tenantId -o tsv)

      # Create or rotate client secret
      SECRET=$(az ad app credential reset --id "$APP_ID" --display-name "bicep-managed" --query password -o tsv)

      echo "{\"appId\":\"$APP_ID\",\"spObjectId\":\"$SP_OBJECT_ID\",\"tenantId\":\"$TENANT_ID\",\"clientSecret\":\"$SECRET\"}" > $AZ_SCRIPTS_OUTPUT_PATH
    '''
  }
}

var logAppReg = logAppRegistrationScript.properties.outputs

// ── Core infrastructure modules ───────────────────────────────────────────────

module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: {
    name: '${prefix}acr'
    location: location
  }
}

module postgres 'modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    name: '${prefix}-pg'
    location: location
    adminPassword: postgresAdminPassword
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: '${prefix}stor'
    location: location
  }
}

// ── Logging infrastructure ────────────────────────────────────────────────────

module logging 'modules/logging.bicep' = {
  name: 'logging'
  params: {
    prefix: prefix
    location: location
    logIngestionPrincipalObjectId: logAppReg.spObjectId
  }
}

// ── App Service ───────────────────────────────────────────────────────────────

module appservice 'modules/appservice.bicep' = {
  name: 'appservice'
  params: {
    name: '${prefix}-api'
    location: location
    acrLoginServer: acr.outputs.loginServer
    acrUsername: acr.outputs.adminUsername
    acrPassword: acr.outputs.adminPassword
    postgresConnectionString: 'Host=${postgres.outputs.fqdn};Database=tinyheroes;Username=${postgres.outputs.administratorLogin};Password=${postgresAdminPassword};SslMode=Require'
    jwtSecret: jwtSecret
    storageConnectionString: storage.outputs.connectionString
    huggingFaceApiKey: huggingFaceApiKey
    allowedOrigin: 'https://mytinyheroes.net'
    logDceEndpoint:    logging.outputs.dceEndpoint
    logDcrImmutableId: logging.outputs.dcrImmutableId
    logTenantId:       logAppReg.tenantId
    logClientId:       logAppReg.appId
    logClientSecret:   logAppReg.clientSecret
  }
}

module swa 'modules/swa.bicep' = {
  name: 'swa'
  params: {
    name: '${prefix}-frontend'
    location: swaLocation
    repositoryUrl: repositoryUrl
    repositoryToken: repositoryToken
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output acrLoginServer string = acr.outputs.loginServer
output acrUsername string = acr.outputs.adminUsername
output appServiceHostname string = appservice.outputs.hostname
output appServiceName string = appservice.outputs.name
output swaHostname string = swa.outputs.hostname
output postgresFqdn string = postgres.outputs.fqdn
output logWorkspaceId string = logging.outputs.workspaceId
output logDceEndpoint string = logging.outputs.dceEndpoint
output logDcrImmutableId string = logging.outputs.dcrImmutableId
output logAppId string = logAppReg.appId
output logTenantId string = logAppReg.tenantId
