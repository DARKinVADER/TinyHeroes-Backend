using './main.bicep'

param location = 'northeurope'
param prefix = 'tinyheroes'
param repositoryUrl = 'https://github.com/DARKinVADER/TinyHeroes'

// Secrets are passed via CLI --parameters flags — never committed here:
// postgresAdminPassword, jwtSecret, repositoryToken, huggingFaceApiKey
