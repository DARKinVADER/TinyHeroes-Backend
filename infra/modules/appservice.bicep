param name string
param location string
param acrLoginServer string
param acrUsername string

@secure()
param acrPassword string

@secure()
param postgresConnectionString string

@secure()
param jwtSecret string

@secure()
param storageConnectionString string

@secure()
param huggingFaceApiKey string

param allowedOrigin string

// Structured logging — Azure Logs Ingestion API (DCE/DCR)
param logDceEndpoint string
param logDcrImmutableId string

@secure()
param logTenantId string

@secure()
param logClientId string

@secure()
param logClientSecret string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${name}-plan'
  location: location
  kind: 'Linux'
  properties: {
    reserved: true
  }
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: name
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      linuxFxVersion: 'DOCKER|${acrLoginServer}/tinyheroes-api:latest'
      appSettings: [
        { name: 'DOCKER_REGISTRY_SERVER_URL', value: 'https://${acrLoginServer}' }
        { name: 'DOCKER_REGISTRY_SERVER_USERNAME', value: acrUsername }
        { name: 'DOCKER_REGISTRY_SERVER_PASSWORD', value: acrPassword }
        { name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE', value: 'false' }
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'ConnectionStrings__Default', value: postgresConnectionString }
        { name: 'Jwt__Secret', value: jwtSecret }
        { name: 'Jwt__Issuer', value: 'tinyheroes-api' }
        { name: 'Jwt__Audience', value: 'tinyheroes-frontend' }
        { name: 'Jwt__ExpiryMinutes', value: '60' }
        { name: 'Storage__ConnectionString', value: storageConnectionString }
        { name: 'AiImage__HuggingFace__ApiKey', value: huggingFaceApiKey }
        { name: 'AiImage__HuggingFace__Model', value: 'black-forest-labs/FLUX.1-schnell' }
        { name: 'AllowedOrigins__0', value: allowedOrigin }
        { name: 'Serilog__WriteTo__1__Args__credentials__endpoint',      value: logDceEndpoint     }
        { name: 'Serilog__WriteTo__1__Args__credentials__immutableId',   value: logDcrImmutableId  }
        { name: 'Serilog__WriteTo__1__Args__credentials__tenantId',      value: logTenantId        }
        { name: 'Serilog__WriteTo__1__Args__credentials__clientId',      value: logClientId        }
        { name: 'Serilog__WriteTo__1__Args__credentials__clientSecret',  value: logClientSecret    }
      ]
    }
  }
}

output hostname string = webApp.properties.defaultHostName
output name string = webApp.name
