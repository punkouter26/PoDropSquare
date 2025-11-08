# Phase 2: Azure Infrastructure Guide

> **Status**: ✅ Ready for local development with Azurite, 🚧 Azure deployment configured

## Overview

PoDropSquare uses a **hybrid local/cloud infrastructure** approach:
- **Local Development**: Azurite emulator + local Application Insights simulation
- **Azure Production**: Azure Table Storage + Application Insights + Log Analytics

## 🏗️ Infrastructure Architecture

### Local Development Stack
```
┌─────────────────────────────────────────┐
│  Developer Machine (localhost)          │
├─────────────────────────────────────────┤
│  Blazor WASM (Browser)                  │
│  ↓ HTTP                                 │
│  ASP.NET Core API (:5000)               │
│  ↓ Connection String                    │
│  Azurite Table Storage (:10002)         │
│  ↓ Telemetry (Console/File)             │
│  Serilog Logs (logs/*.txt)              │
└─────────────────────────────────────────┘
```

### Azure Production Stack
```
┌─────────────────────────────────────────┐
│  Azure Cloud                            │
├─────────────────────────────────────────┤
│  Browser (Any)                          │
│  ↓ HTTPS                                │
│  App Service (PoDropSquare)             │
│  ├─ ASP.NET Core 9.0                    │
│  ├─ Blazor WASM (hosted)                │
│  ↓ Managed Identity                     │
│  Azure Table Storage                    │
│  ↓ Telemetry                            │
│  Application Insights                   │
│  ↓ Logs                                 │
│  Log Analytics Workspace                │
└─────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) (for cloud deployment)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- [Node.js](https://nodejs.org/) (for Azurite)

### 1. Install Azurite (One-time setup)

```powershell
# Install Azurite globally via npm
npm install -g azurite

# Verify installation
azurite --version
```

### 2. Start Azurite

**Option A: PowerShell (Terminal 1)**
```powershell
# Start Azurite with custom data location
azurite --silent --location c:\azurite --debug c:\azurite\debug.log

# Or use workspace location
azurite --silent --location . --debug .\azurite-debug.log
```

**Option B: Background Process**
```powershell
Start-Process azurite -ArgumentList "--silent", "--location", "c:\azurite" -WindowStyle Hidden
```

**Option C: VS Code Task** (Add to `.vscode/tasks.json`)
```json
{
  "label": "Start Azurite",
  "type": "shell",
  "command": "azurite",
  "args": ["--silent", "--location", "${workspaceFolder}"],
  "isBackground": true,
  "problemMatcher": []
}
```

### 3. Verify Azurite is Running

```powershell
# Check process
Get-Process azurite

# Test connection (should return XML blob list)
curl http://127.0.0.1:10000/devstoreaccount1?comp=list

# Check tables endpoint
curl http://127.0.0.1:10002/devstoreaccount1/Tables
```

### 4. Run the Application

```powershell
# Terminal 2 (with Azurite running in Terminal 1)
cd backend/src/Po.PoDropSquare.Api
dotnet run

# App will be available at:
# http://localhost:5000
# https://localhost:5001
```

### 5. Verify Local Setup

**Check health endpoint:**
```powershell
# Should return HTTP 200 with storage health check
curl http://localhost:5000/api/health
```

**Check diagnostics page:**
```
Open browser: http://localhost:5000/diag
Expected: "Healthy" status with storage component green
```

**Check logs:**
```powershell
# Real-time log monitoring
Get-Content backend/src/Po.PoDropSquare.Api/logs/podropsquare-*.txt -Wait -Tail 20
```

## ☁️ Azure Deployment

### Option 1: Deploy with Azure Developer CLI (Recommended)

```powershell
# Login to Azure
azd auth login

# Provision infrastructure + deploy app (first time)
azd up

# Subsequent deploys (code changes only)
azd deploy

# View logs
azd monitor --logs

# View all resources
azd show
```

### Option 2: Deploy with Azure CLI

```powershell
# Login
az login

# Set subscription
az account set --subscription "Your Subscription Name"

# Deploy infrastructure
az deployment sub create `
  --location eastus2 `
  --template-file infra/main.bicep `
  --parameters environmentName=prod

# Get App Service name from outputs
$appName = (az deployment sub show --name main --query properties.outputs.APP_SERVICE_NAME.value -o tsv)

# Deploy application
dotnet publish backend/src/Po.PoDropSquare.Api -c Release -o publish
Compress-Archive -Path publish/* -DestinationPath app.zip -Force
az webapp deploy --name $appName --resource-group PoDropSquare --src-path app.zip --type zip
```

## 📁 Infrastructure Files

### `infra/main.bicep`
- **Purpose**: Subscription-level deployment orchestration
- **Creates**: Resource Group (PoDropSquare)
- **Calls**: resources.bicep module
- **Parameters**:
  - `environmentName`: Environment identifier (dev/prod)
  - `location`: Azure region (default: eastus2)
  - `principalId`: User/SP for RBAC assignments

### `infra/resources.bicep`
- **Purpose**: Core Azure resources
- **Creates**:
  - Storage Account (Azure Table Storage)
  - Log Analytics Workspace
  - Application Insights
  - App Service (uses existing PoShared plan)
- **Configuration**:
  - Storage: Standard_LRS (cheapest)
  - App Service: .NET 9.0, HTTPS only
  - Retention: 30 days (App Insights + Log Analytics)

### `azure.yaml`
- **Purpose**: Azure Developer CLI configuration
- **Defines**: Service-to-project mapping
- **Service**: `api` → `./backend/src/Po.PoDropSquare.Api`

## 🔐 Configuration Management

### Local Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "AzureTableStorage": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;..."
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=00000000-0000-0000-0000-000000000000;..."
  }
}
```

### Azure Production (App Service Configuration)
- ✅ Managed via Bicep (`appSettings` in resources.bicep)
- ✅ Secrets stored in App Service Configuration (never in code)
- ✅ Connection strings use Key Vault references (future enhancement)

## 🧪 Testing the Setup

### 1. Azurite Health Check
```powershell
# Start Azurite
azurite --silent --location .

# In another terminal, run API
dotnet run --project backend/src/Po.PoDropSquare.Api

# Test storage connection
curl http://localhost:5000/api/health | ConvertFrom-Json | Select-Object status
# Expected: "status": "Healthy"
```

### 2. Submit Test Score
```powershell
# Submit a test score
$body = @{
    playerName = "TEST"
    score = 1500
    timestamp = (Get-Date).ToString("o")
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/scores" -Method POST -Body $body -ContentType "application/json"
```

### 3. Verify in Azurite
```powershell
# Install Azure Storage Explorer or use REST API
curl "http://127.0.0.1:10002/devstoreaccount1/Tables"
```

## 📊 Monitoring

### Local Development
- **Console Logs**: Real-time in terminal
- **File Logs**: `backend/src/Po.PoDropSquare.Api/logs/podropsquare-*.txt`
- **Debug Logs**: `backend/src/Po.PoDropSquare.Api/DEBUG/server_log.txt`

### Azure Production
- **Application Insights**: Live metrics, traces, exceptions
- **Log Analytics**: KQL queries (see `docs/KQL-QUERIES.md`)
- **App Service Logs**: Deployment logs, stdout/stderr

### Key Metrics to Monitor
```kql
// Top 5 slowest API requests (last 24h)
requests
| where timestamp > ago(24h)
| summarize AvgDuration = avg(duration), Count = count() by name
| order by AvgDuration desc
| take 5
```

## 🛠️ Troubleshooting

### Issue: "Unable to connect to Azure Table Storage"

**Cause**: Azurite not running

**Solution**:
```powershell
# Check if Azurite is running
Get-Process azurite

# If not running, start it
azurite --silent --location .
```

### Issue: "Rate limit exceeded" (429 errors in tests)

**Cause**: Integration tests hitting rate limit (20 req/min in dev)

**Solution**: Adjust rate limit in `appsettings.Development.json`:
```json
{
  "RateLimit": {
    "MaxRequests": 100,  // Higher limit for testing
    "WindowDuration": "00:01:00"
  }
}
```

### Issue: "Application Insights connection failed"

**Cause**: Using local dev connection string (expected behavior)

**Solution**: This is normal in local dev. Production uses real App Insights.

### Issue: E2E tests fail with Playwright errors

**Cause**: Playwright browsers not installed

**Solution**:
```powershell
# Install Playwright browsers
dotnet build backend/tests/Po.PoDropSquare.E2E.Tests
pwsh backend/tests/Po.PoDropSquare.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install
```

### Issue: `azd up` fails with "Principal not found"

**Cause**: User identity needs Storage RBAC role

**Solution**:
```powershell
# Get your user principal ID
$principalId = (az ad signed-in-user show --query id -o tsv)

# Deploy with principal ID
azd up --parameter principalId=$principalId
```

## 🔄 CI/CD Integration

### GitHub Actions (Phase 4)
- **Trigger**: Push to `main` branch
- **Steps**:
  1. Build .NET solution
  2. Run unit tests (excluding E2E)
  3. Provision Azure resources (`azd provision`)
  4. Deploy application (`azd deploy`)
  5. Run smoke tests against production

### Manual Deployment
```powershell
# Quick deploy (skip tests)
azd deploy --no-prompt

# Full CI/CD simulation
dotnet test --filter "FullyQualifiedName!~E2E"  # Run tests
azd deploy                                       # Deploy app
```

## 📚 Next Steps

- [x] **Phase 1**: Documentation & Packages ✅
- [x] **Phase 2**: Infrastructure Setup ✅
- [ ] **Phase 3**: Testing & Coverage (Traits, Playwright, 80% coverage)
- [ ] **Phase 4**: CI/CD Pipeline (GitHub Actions, CodeQL, OIDC)
- [ ] **Phase 5**: Advanced Telemetry (Custom spans, metrics, KQL library)

## 📖 Additional Resources

- [Azurite Documentation](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
- [Azure Developer CLI Docs](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Azure Table Storage .NET SDK](https://learn.microsoft.com/azure/storage/tables/table-storage-how-to-use-dotnet)
- [Application Insights for ASP.NET Core](https://learn.microsoft.com/azure/azure-monitor/app/asp-net-core)
- [Bicep Language Reference](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
