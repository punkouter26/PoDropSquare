# PoDropSquare - Physics-Based Tower Building Game

> A fast-paced physics game where players race against dual timers to build stable towers by dropping colored blocks. Built with .NET 9.0, Blazor WebAssembly, and Matter.js physics engine.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4)](https://blazor.net/)
[![Azure](https://img.shields.io/badge/Azure-Deployed-0078D4)](https://azure.microsoft.com/)
[![CI/CD](https://img.shields.io/badge/CI%2FCD-GitHub%20Actions-2088FF)](https://github.com/features/actions)

## 🎮 What is PoDropSquare?

PoDropSquare is a modern browser-based physics game that combines quick reflexes with strategic thinking. Players must:
- **Drop colored squares** to build increasingly tall towers
- **Beat the clock** with a 20-second survival timer
- **React quickly** to the 2-second danger countdown between drops
- **Master physics** - gravity, collisions, and stability matter!
- **Compete globally** on the Azure-backed leaderboard

Perfect for quick gaming sessions and competitive play!

## 🚀 Quick Start - Run Locally

### Prerequisites

| Requirement | Version | Download |
|-------------|---------|----------|
| **.NET SDK** | 9.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/9.0) |
| **Node.js** | 18+ | [Download](https://nodejs.org/) (for Azurite) |
| **Azure Developer CLI** | Latest | [Download](https://aka.ms/azd-install) (optional, for deployment) |

### Option 1: Automated Setup (Recommended)

```powershell
# 1. Install Azurite (one-time)
npm install -g azurite

# 2. Start everything (Azurite + API + Browser)
.\scripts\start-local-dev.ps1

# 3. Stop when done
.\scripts\stop-local-dev.ps1

# 4. Verify setup anytime
.\scripts\test-local-setup.ps1
```

### Option 2: Manual Setup

1. **Start Azurite** (Terminal 1)
   ```powershell
   azurite --silent --location .
   ```

2. **Run Application** (Terminal 2)
   ```powershell
   dotnet run --project backend/src/Po.PoDropSquare.Api
   ```

3. **Open Browser**
   - 🌐 **App**: http://localhost:5000
   - 🩺 **Health**: http://localhost:5000/diag
   - 📖 **API Docs**: http://localhost:5000/swagger

### Testing the Application

```bash
# Run all tests (unit, integration, E2E)
dotnet test

# Run specific test project
dotnet test backend/tests/Po.PoDropSquare.Api.Tests/

# Generate code coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 📁 Project Structure

```
PoDropSquare/
├── backend/
│   ├── src/
│   │   ├── Po.PoDropSquare.Api/          # ASP.NET Core API (hosts Blazor WASM)
│   │   ├── Po.PoDropSquare.Core/         # Domain entities & contracts
│   │   ├── Po.PoDropSquare.Data/         # Azure Table Storage repository
│   │   └── Po.PoDropSquare.Services/     # Business logic services
│   └── tests/
│       ├── Po.PoDropSquare.Api.Tests/    # API integration tests (xUnit)
│       ├── Po.PoDropSquare.Core.Tests/   # Unit tests
│       └── Po.PoDropSquare.E2E.Tests/    # End-to-end tests (Playwright)
├── frontend/
│   ├── src/
│   │   └── Po.PoDropSquare.Blazor/       # Blazor WebAssembly SPA
│   └── tests/
│       └── Po.PoDropSquare.Blazor.Tests/ # Component tests (bUnit)
├── infra/
│   ├── main.bicep                        # Azure infrastructure as code
│   ├── resources.bicep                   # Resource definitions
│   └── main.parameters.json              # Bicep parameters
├── docs/
│   ├── KQL-QUERIES.md                    # 31 Application Insights queries
│   ├── APPLICATION-INSIGHTS-SETUP.md     # Telemetry configuration guide
│   └── PHASE4-SUMMARY.md                 # Monitoring implementation summary
├── .github/
│   └── workflows/
│       └── azure-dev.yml                 # CI/CD pipeline (OIDC auth)
├── PRD.MD                                # Product Requirements Document
├── azure.yaml                            # Azure Developer CLI config
└── PoDropSquare.http                     # REST client test requests
```

## 🛠️ Technology Stack

### Backend
- **.NET 9.0** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful endpoints with Swagger/OpenAPI
- **Azure Table Storage** - NoSQL data persistence (leaderboard)
- **Serilog** - Structured logging with file and console sinks
- **Azurite** - Local Azure Storage emulator
- **Health Checks** - Custom Azure Table Storage health check

### Frontend
- **Blazor WebAssembly** - C# in the browser via WebAssembly
- **Matter.js** - 2D physics engine via JavaScript interop
- **Radzen.Blazor** - UI component library
- **HTML5 Canvas** - High-performance rendering

### Testing
- **xUnit** - Unit and integration testing framework
- **Microsoft.AspNetCore.Mvc.Testing** - WebApplicationFactory for API tests
- **bUnit** - Blazor component testing
- **Playwright** - Cross-browser E2E testing (Chromium)

### Infrastructure & Deployment
- **Azure Bicep** - Infrastructure as Code
- **Azure Developer CLI (azd)** - Deployment automation
- **GitHub Actions** - CI/CD with OIDC authentication (no secrets!)
- **Application Insights** - Telemetry and monitoring
- **Log Analytics** - Centralized log aggregation

## 📡 API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/scores` | Submit game score |
| `GET` | `/api/scores` | Retrieve player's score history |
| `GET` | `/api/scores/top10` | Top 10 global leaderboard |
| `GET` | `/api/scores/leaderboard?startRank=1&count=50` | Paginated leaderboard |
| `GET` | `/api/scores/player/{playerName}/rank` | Player's global rank |
| `POST` | `/api/log/client` | Client-side error logging |
| `POST` | `/api/log/error` | Server-side error logging |
| `GET` | `/api/health` | Health check (includes Azure Table Storage) |

📘 **Full API Documentation**: Available at `/swagger` when running API project

📄 **REST Client Tests**: See `PoDropSquare.http` for 50+ manual test requests

## 🌐 Deployment

### Azure Deployment (Recommended)

The project uses **Azure Developer CLI (azd)** for one-command deployment:

```bash
# Login to Azure
azd auth login

# Provision infrastructure + deploy application
azd up

# Just deploy code (infrastructure already exists)
azd deploy
```

**What gets deployed:**
- Azure App Service (F1 Free tier)
- Azure Storage Account (Table Storage for leaderboard)
- Application Insights (monitoring and telemetry)
- Log Analytics Workspace (centralized logging)

**Cost:** ~$10-20/month (F1 App Service is free, storage + insights minimal)

### CI/CD Pipeline

GitHub Actions workflow automatically:
1. **Build** - Compile .NET solution, run unit tests
2. **Deploy** - Provision Azure resources, deploy to App Service
3. **E2E Test** - Run Playwright tests against live deployment
4. **Health Check** - Verify `/api/health` endpoint

**Setup Instructions:** See `.github/CICD-SETUP.md`

**Security:** Uses OIDC federated credentials (no long-lived secrets in GitHub!)

## 📊 Monitoring & Observability

### Application Insights Integration

The application sends telemetry to Azure Application Insights:
- Request/response metrics
- Exception tracking
- Custom events (game scores, user actions)
- Client-side errors
- Performance counters

### KQL Queries (31 Production-Ready Queries)

📘 **Full Query Library**: `docs/KQL-QUERIES.md`

**Quick Reference** (top 5 critical queries):

```kql
// 1. Top 10 Slowest API Requests (last 24h)
requests
| where timestamp > ago(24h)
| summarize AvgDuration = avg(duration), Count = count() by operation_Name
| top 10 by AvgDuration desc

// 2. Error Rate by Hour (last 7 days)
requests
| where timestamp > ago(7d)
| summarize Total = count(), Failed = countif(success == false) by bin(timestamp, 1h)
| extend ErrorRate = (Failed * 100.0) / Total

// 3. Active Users (last 24h)
pageViews
| where timestamp > ago(24h)
| summarize Users = dcount(user_Id)

// 4. JavaScript Errors (client-side, last 24h)
exceptions
| where timestamp > ago(24h) and client_Type == "Browser"
| summarize Count = count() by problemId, outerMessage

// 5. Health Check Monitoring
requests
| where timestamp > ago(1h) and name == "GET /api/health"
| summarize FailureCount = countif(success == false)
```

**Alert Configuration:** See `docs/APPLICATION-INSIGHTS-SETUP.md` for 3 critical alerts

## 🧪 Testing Strategy

This project follows **Test-Driven Development (TDD)**:

1. **Red** - Write failing test
2. **Green** - Implement minimum code to pass
3. **Refactor** - Improve while keeping tests green

### Test Coverage

- **48+ Unit/Integration Tests** - API endpoints, business logic, data access
- **11 E2E Tests** - Full user workflows with Playwright
- **50+ Manual Tests** - REST client requests in `PoDropSquare.http`

### Running Tests

```bash
# All tests
dotnet test

# API integration tests (48+ tests)
dotnet test backend/tests/Po.PoDropSquare.Api.Tests/

# Blazor component tests
dotnet test frontend/tests/Po.PoDropSquare.Blazor.Tests/

# E2E tests (requires app running)
dotnet test backend/tests/Po.PoDropSquare.E2E.Tests/
```

**Note:** E2E tests run automatically in CI/CD pipeline after deployment

## ⚙️ Configuration

### Development (Local)

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "AzureTableStorage": "UseDevelopmentStorage=true"  // Azurite emulator
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/log-.txt" } }
    ]
  }
}
```

### Production (Azure)

```json
// appsettings.json
{
  "ConnectionStrings": {
    "AzureTableStorage": "DefaultEndpointsProtocol=https;AccountName=..."
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=..."  // Set by Azure deployment
  }
}
```

**Environment Variables** (set in Azure App Service):
- `APPLICATIONINSIGHTS_CONNECTION_STRING` - Auto-configured by Bicep deployment
- `ConnectionStrings__AzureTableStorage` - Storage account connection string

## 🎯 Performance Targets

| Metric | Target | Actual |
|--------|--------|--------|
| Game FPS | 60 FPS | ✅ Validated |
| Input Response | <50ms | ✅ Validated |
| Initial Load | <3s | ✅ Validated |
| API Response | <200ms | ✅ Monitored via App Insights |
| Uptime | 99.9% | 📊 Tracked in Azure |

## 🤝 Contributing

### Coding Principles (See `.github/copilot-instructions.md`)

1. **Automate with CLI** - Use `dotnet`, `az`, `gh`, `git`, `azd`
2. **SOLID Principles** - Single Responsibility, Open/Closed, etc.
3. **Clean Architecture** - Organize by vertical slices or simple services
4. **Maintain Simplicity** - Keep code clean, concise, easy to understand
5. **TDD** - Write tests first, then implementation
6. **Observability** - Structured logging with Serilog
7. **Proactive Refactoring** - Files >500 lines should be split

### Development Workflow

1. Check `STEPS.MD` for next feature to implement
2. Write failing test(s) for the feature
3. Implement code to pass tests
4. Refactor while keeping tests green
5. Build UI against verified API
6. Mark step complete in `STEPS.MD`
7. Submit PR with test evidence

## 📚 Documentation

| Document | Description |
|----------|-------------|
| **[docs/PRD.MD](docs/PRD.MD)** | Product Requirements Document (complete spec) |
| **STEPS.MD** | 10 high-level implementation steps |
| **[docs/KQL-QUERIES.md](docs/KQL-QUERIES.md)** | 31 Application Insights queries |
| **[docs/APPLICATION-INSIGHTS-SETUP.md](docs/APPLICATION-INSIGHTS-SETUP.md)** | Telemetry setup guide |
| **docs/PHASE1-IMPLEMENTATION-SUMMARY.md** | Phase 1 completion summary |
| **.github/copilot-instructions.md** | AI coding agent rules |
| **PoDropSquare.http** | REST client manual tests |

## 🗺️ Roadmap

See **[docs/PRD.MD](docs/PRD.MD)** for detailed roadmap. Upcoming phases:

- **Phase 2 (Q2 2025)** - Multiplayer mode, spectator system
- **Phase 3 (Q3 2025)** - Tournament system, ranked matches
- **Phase 4 (Q4 2025)** - Mobile apps (iOS, Android)
- **Phase 5 (Q1 2026)** - Monetization, premium features

## 📜 License

[Your License Here]

## 🙏 Acknowledgments

- **Matter.js** - Excellent 2D physics engine
- **Radzen.Blazor** - Beautiful Blazor components
- **Azure** - Reliable cloud infrastructure
- **GitHub Actions** - Seamless CI/CD automation

---

**Built with ❤️ using .NET 9.0 and Blazor WebAssembly**