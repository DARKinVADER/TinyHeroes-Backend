using './main.integration.bicep'

param location = 'northeurope'
param prefix = 'tinyheroes'
param repositoryUrl = 'https://github.com/DARKinVADER/TinyHeroes'
param integrationBranch = 'integration'

// Secrets are passed via CLI --parameters flags — never committed here:
// postgresAdminPassword, jwtSecret, repositoryToken
