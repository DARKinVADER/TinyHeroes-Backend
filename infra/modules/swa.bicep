param name string
param location string
param repositoryUrl string
param branch string = 'main'

@secure()
param repositoryToken string

resource swa 'Microsoft.Web/staticSites@2023-01-01' = {
  name: name
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    repositoryUrl: repositoryUrl
    branch: branch
    repositoryToken: repositoryToken
    buildProperties: {
      appLocation: 'frontend'
      outputLocation: 'dist/frontend/browser'
      skipGithubActionWorkflowGeneration: true
    }
  }
}

output hostname string = swa.properties.defaultHostname

@secure()
output deploymentToken string = swa.listSecrets().properties.apiKey
