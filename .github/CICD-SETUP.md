# GitHub Actions CI/CD Setup Guide

This guide explains how to configure GitHub Actions for automated deployment to Azure using Azure Developer CLI (azd).

## Prerequisites

- Azure subscription
- Azure CLI installed locally
- Azure Developer CLI (azd) installed locally
- GitHub repository

## Setup Steps

### 1. Create Azure Service Principal with Federated Credentials

```powershell
# Set variables
$subscriptionId = "YOUR_SUBSCRIPTION_ID"
$resourceGroupName = "rg-podropsquare-dev-eastus"
$appName = "gh-podropsquare-deploy"
$repoOwner = "punkouter26"
$repoName = "PoDropSquare"

# Login to Azure
az login
az account set --subscription $subscriptionId

# Create service principal with federated credential for main branch
az ad sp create-for-rbac --name $appName `
  --role Contributor `
  --scopes /subscriptions/$subscriptionId `
  --sdk-auth `
  --json-auth

# Note the output - you'll need clientId, tenantId, subscriptionId
```

### 2. Configure Federated Identity Credentials

```powershell
# Get the Application (client) ID from previous step
$appId = "YOUR_APP_ID_FROM_PREVIOUS_STEP"

# Add federated credential for main branch
az ad app federated-credential create `
  --id $appId `
  --parameters @- << EOF
{
  "name": "PoDropSquare-Main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:$repoOwner/${repoName}:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF

# Add federated credential for feature branch
az ad app federated-credential create `
  --id $appId `
  --parameters @- << EOF
{
  "name": "PoDropSquare-Feature",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:$repoOwner/${repoName}:ref:refs/heads/001-podropsquare-is-a",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF

# Add federated credential for pull requests
az ad app federated-credential create `
  --id $appId `
  --parameters @- << EOF
{
  "name": "PoDropSquare-PR",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:$repoOwner/${repoName}:pull_request",
  "audiences": ["api://AzureADTokenExchange"]
}
EOF
```

### 3. Configure GitHub Secrets

Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

Add the following secrets:

| Secret Name | Description | Example Value |
|------------|-------------|---------------|
| `AZURE_CLIENT_ID` | Application (client) ID | `12345678-1234-1234-1234-123456789012` |
| `AZURE_TENANT_ID` | Directory (tenant) ID | `87654321-4321-4321-4321-210987654321` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `abcdef12-3456-7890-abcd-ef1234567890` |
| `AZURE_ENV_NAME` | azd environment name | `podropsquare-dev` |
| `AZURE_LOCATION` | Azure region | `eastus` |

### 4. Test the Deployment

#### Option A: Using azd locally

```powershell
# Initialize azd environment
azd init

# Set environment variables
azd env set AZURE_SUBSCRIPTION_ID "YOUR_SUBSCRIPTION_ID"
azd env set AZURE_LOCATION "eastus"

# Provision and deploy
azd up
```

#### Option B: Trigger GitHub Actions

```powershell
# Commit and push to trigger workflow
git add .
git commit -m "feat: add GitHub Actions CI/CD"
git push origin 001-podropsquare-is-a
```

### 5. Verify Deployment

After the GitHub Actions workflow completes:

1. Check the **Actions** tab in your GitHub repository
2. Verify all jobs (build, deploy, e2e-tests) passed
3. Access the deployed application URL from the deployment logs
4. Test the health endpoint: `https://YOUR_APP_URL/api/health`

## Workflow Structure

The `.github/workflows/azure-dev.yml` workflow has three jobs:

### Job 1: Build
- Restores NuGet packages
- Builds the solution in Release mode
- Runs unit tests (API and Blazor)
- Publishes the API project
- Uploads build artifacts

### Job 2: Deploy
- Downloads build artifacts
- Logs into Azure using OIDC (no secrets needed!)
- Provisions Azure resources using Bicep
- Deploys the application using azd
- Verifies deployment with health check

### Job 3: E2E Tests
- Installs Playwright browsers
- Runs end-to-end tests against deployed app
- Uploads test results

## Troubleshooting

### Federated credential not working
- Ensure the subject format is exactly: `repo:OWNER/REPO:ref:refs/heads/BRANCH`
- Wait a few minutes after creating credentials (propagation delay)

### azd provision fails
- Check `AZURE_LOCATION` matches a valid Azure region
- Ensure service principal has Contributor role on subscription
- Verify Bicep files are valid: `azd provision --what-if`

### E2E tests fail
- Check if application is actually running: `curl https://YOUR_APP/api/health`
- Review test logs in GitHub Actions artifacts
- Verify Playwright browsers installed correctly

## Security Best Practices

✅ **Using OIDC** - No long-lived secrets stored in GitHub  
✅ **Minimal permissions** - Service principal has only Contributor role  
✅ **Branch protection** - Deploy only from protected branches  
✅ **Environment approval** - Use GitHub environments for production  

## Useful Commands

```powershell
# View azd environment values
azd env get-values

# View provisioned resources
azd show

# View logs
azd monitor

# Cleanup resources
azd down
```

## Next Steps

1. ✅ Configure GitHub Secrets
2. ✅ Test deployment locally with `azd up`
3. ✅ Push code to trigger GitHub Actions
4. ⬜ Add environment protection rules
5. ⬜ Configure notification for failed deployments
6. ⬜ Add staging environment for PR previews
