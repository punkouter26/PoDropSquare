# AI Coding Agent Guide for PoDropSquare

> Best practices and context for AI coding assistants working with the PoDropSquare codebase

## 🎯 Project Overview

**PoDropSquare** is a physics-based tower building game where players race against dual timers (20s survival + 2s danger countdown) to build stable towers by dropping colored blocks. The application is built with:

- **Frontend**: Blazor WebAssembly with Matter.js physics engine
- **Backend**: ASP.NET Core Web API (.NET 9.0)
- **Data**: Azure Table Storage (leaderboard persistence)
- **Infrastructure**: Azure Bicep, deployed via Azure Developer CLI (azd)
- **CI/CD**: GitHub Actions with OIDC authentication
- **Monitoring**: Application Insights + Serilog structured logging

## 📋 Guiding Principles (See `.github/copilot-instructions.md`)

### 1. Automate with CLI Tools
Always prefer CLI automation over manual steps:
- `dotnet` - Build, test, run, add packages
- `az` - Azure resource management
- `azd` - Azure deployment automation
- `gh` - GitHub CLI for repo operations
- `git` - Version control

**Example:**
```bash
# ✅ Good - CLI automation
dotnet add package Serilog.Sinks.ApplicationInsights
dotnet build
dotnet test

# ❌ Bad - Manual file editing
# Manually editing .csproj to add PackageReference
```

### 2. Follow SOLID Principles
- **Single Responsibility** - Each class has one reason to change
- **Open/Closed** - Open for extension, closed for modification
- **Liskov Substitution** - Derived classes must be substitutable
- **Interface Segregation** - Many specific interfaces > one general
- **Dependency Inversion** - Depend on abstractions, not concretions

**Example:**
```csharp
// ✅ Good - Single Responsibility
public class ScoreRepository { /* only handles data access */ }
public class ScoreService { /* only handles business logic */ }
public class ScoresController { /* only handles HTTP */ }

// ❌ Bad - Multiple responsibilities
public class ScoreManager {
    public void SaveToDatabase() { }
    public void CalculateScore() { }
    public IActionResult HandleHttpRequest() { }
}
```

### 3. Maintain Simplicity
- **Keep files under ~500 lines** - Refactor larger files into focused components
- **Remove unused code** - Delete dead code regularly
- **Avoid over-abstraction** - Use built-in framework features before creating custom abstractions
- **Prefer composition over inheritance** - Use services/components over deep class hierarchies

### 4. Test-Driven Development (TDD)
**Always follow the TDD cycle:**

1. **🔴 Red** - Write a failing test
2. **🟢 Green** - Implement minimum code to pass
3. **♻️ Refactor** - Improve while keeping tests green

**Example workflow:**
```bash
# 1. Write failing test
# File: ScoreServiceTests.cs
[Fact]
public void CalculateScore_WithStableBlocks_ReturnsHighScore() {
    // Arrange
    var service = new ScoreService();
    var blocks = CreateStableBlocks();
    
    // Act
    var score = service.CalculateScore(blocks);
    
    // Assert
    Assert.True(score > 1000);  // ❌ FAILS - not implemented yet
}

# 2. Run test to see it fail
dotnet test --filter "CalculateScore_WithStableBlocks_ReturnsHighScore"

# 3. Implement minimum code
public int CalculateScore(List<Block> blocks) {
    return blocks.Sum(b => b.StabilityPoints);
}

# 4. Run test again
dotnet test --filter "CalculateScore_WithStableBlocks_ReturnsHighScore"  # ✅ PASSES

# 5. Refactor if needed
public int CalculateScore(List<Block> blocks) {
    const int StabilityMultiplier = 10;
    return blocks.Sum(b => b.StabilityPoints * StabilityMultiplier);
}

# 6. Verify tests still pass
dotnet test
```

### 5. Always Run API Project (Not Blazor Standalone)
The API project **hosts** the Blazor WebAssembly application at ports:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`

**✅ Correct way to run the app:**
```bash
dotnet run --project backend/src/Po.PoDropSquare.Api
# App available at http://localhost:5000
```

**❌ Incorrect - Don't run Blazor standalone:**
```bash
# This won't work - CORS issues, API not available
dotnet run --project frontend/src/Po.PoDropSquare.Blazor
```

## 🏗️ Architecture Patterns

### Project Organization

The codebase follows **Clean Architecture** with vertical slices:

```
backend/src/
├── Po.PoDropSquare.Api/          # HTTP Layer (Controllers, Middleware)
│   ├── Controllers/              # API endpoints
│   ├── Middleware/               # Global error handling, rate limiting
│   └── Program.cs                # App configuration
├── Po.PoDropSquare.Core/         # Domain Layer (Entities, Contracts)
│   ├── Entities/                 # Domain models (ScoreEntry, LeaderboardEntry)
│   └── Contracts/                # DTOs and API contracts
├── Po.PoDropSquare.Data/         # Data Access Layer
│   └── Repositories/             # Azure Table Storage access
└── Po.PoDropSquare.Services/     # Application Layer (Business Logic)
    └── Services/                 # Score calculation, leaderboard logic

frontend/src/
└── Po.PoDropSquare.Blazor/       # Presentation Layer
    ├── Pages/                    # Routable pages (Home, Game, HighScores)
    ├── Components/               # Reusable UI components
    ├── Services/                 # HTTP clients, state management
    └── Models/                   # Client-side view models
```

### Naming Conventions

- **Projects**: `Po.PoDropSquare.[LayerName]` (e.g., `Po.PoDropSquare.Api`)
- **Azure Tables**: `PoPoDropSquare[TableName]` (e.g., `PoPoDropSquareScores`)
- **Controllers**: Suffix with `Controller` (e.g., `ScoresController`)
- **Services**: Suffix with `Service` (e.g., `ScoreService`)
- **Repositories**: Suffix with `Repository` (e.g., `ScoreRepository`)
- **Tests**: `[ClassName]Tests.cs` (e.g., `ScoreServiceTests.cs`)

### Dependency Flow

```
API Layer (Controllers)
    ↓ depends on
Application Layer (Services)
    ↓ depends on
Domain Layer (Entities, Contracts)
    ↑ referenced by
Data Access Layer (Repositories)
```

**Rule:** Dependencies flow **inward** toward the domain. Domain has **zero** external dependencies.

## 🧪 Testing Strategy

### Test Types

| Type | Framework | Purpose | Example |
|------|-----------|---------|---------|
| **Unit Tests** | xUnit | Test single classes in isolation | `ScoreCalculationTests.cs` |
| **Integration Tests** | xUnit + WebApplicationFactory | Test API endpoints with real infrastructure | `ScoreSubmissionContractTests.cs` |
| **Component Tests** | bUnit | Test Blazor components | `GameTimerTests.cs` |
| **E2E Tests** | Playwright | Test full user workflows | `CoreGameplayE2ETests.cs` |

### Test Naming Convention

```csharp
// Pattern: MethodName_StateUnderTest_ExpectedBehavior

[Fact]
public void CalculateScore_WithZeroBlocks_ReturnsZero() { }

[Fact]
public void CalculateScore_WithStableBlocks_ReturnsPositiveScore() { }

[Fact]
public void SubmitScore_WithInvalidPlayerName_ReturnsBadRequest() { }
```

### Writing Good Tests

**✅ Good test - Arrange/Act/Assert pattern:**
```csharp
[Fact]
public async Task GetTop10_ReturnsOrderedByScoreDescending() {
    // Arrange
    var client = _factory.CreateClient();
    await SeedScores(new[] { 100, 500, 300 });
    
    // Act
    var response = await client.GetAsync("/api/scores/top10");
    var result = await response.Content.ReadFromJsonAsync<List<LeaderboardEntry>>();
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(3, result.Count);
    Assert.Equal(500, result[0].Score);  // Highest first
    Assert.Equal(300, result[1].Score);
    Assert.Equal(100, result[2].Score);
}
```

**❌ Bad test - Testing multiple concerns:**
```csharp
[Fact]
public async Task TestEverything() {
    // Tests submission, retrieval, sorting, pagination all in one
    // Hard to debug when it fails
}
```

### Running Tests

```bash
# All tests
dotnet test

# Specific test project
dotnet test backend/tests/Po.PoDropSquare.Api.Tests/

# Specific test class
dotnet test --filter "ScoreServiceTests"

# Specific test method
dotnet test --filter "CalculateScore_WithStableBlocks_ReturnsHighScore"

# With detailed output
dotnet test --logger "console;verbosity=detailed"
```

**Note:** E2E tests require the application to be running. They run automatically in CI/CD after deployment.

## 🔧 Common Tasks

### Adding a New NuGet Package

```bash
# Add to specific project
dotnet add backend/src/Po.PoDropSquare.Api/Po.PoDropSquare.Api.csproj package Serilog.Sinks.Seq

# Build to verify
dotnet build

# If build succeeds, commit
git add .
git commit -m "Add Serilog.Sinks.Seq for centralized logging"
```

### Adding a New API Endpoint

**Follow TDD workflow:**

1. **Write contract test first:**
```csharp
// File: backend/tests/Po.PoDropSquare.Api.Tests/PlayerStatsContractTests.cs
[Fact]
public async Task GetPlayerStats_ReturnsPlayerStatistics() {
    // Arrange
    var client = _factory.CreateClient();
    await SeedPlayerData("Alice", new[] { 100, 200, 300 });
    
    // Act
    var response = await client.GetAsync("/api/stats/Alice");
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var stats = await response.Content.ReadFromJsonAsync<PlayerStats>();
    Assert.Equal(3, stats.GamesPlayed);
    Assert.Equal(200, stats.AverageScore);
}
```

2. **Run test to see it fail:**
```bash
dotnet test --filter "GetPlayerStats_ReturnsPlayerStatistics"
# ❌ Fails - endpoint doesn't exist yet
```

3. **Implement minimum code:**
```csharp
// File: backend/src/Po.PoDropSquare.Api/Controllers/StatsController.cs
[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase {
    private readonly IStatsService _statsService;
    
    public StatsController(IStatsService statsService) {
        _statsService = statsService;
    }
    
    [HttpGet("{playerName}")]
    public async Task<ActionResult<PlayerStats>> GetPlayerStats(string playerName) {
        var stats = await _statsService.GetPlayerStatsAsync(playerName);
        return Ok(stats);
    }
}
```

4. **Run test again:**
```bash
dotnet test --filter "GetPlayerStats_ReturnsPlayerStatistics"
# ✅ Passes
```

5. **Add to REST client tests:**
```http
### Get Player Statistics
GET {{baseUrl}}/api/stats/Alice
Accept: application/json
```

6. **Build and verify:**
```bash
dotnet build
dotnet test
```

### Adding a New Blazor Component

**Example: Add a countdown timer component**

1. **Write component test first:**
```csharp
// File: frontend/tests/Po.PoDropSquare.Blazor.Tests/CountdownTimerTests.cs
public class CountdownTimerTests : TestContext {
    [Fact]
    public void CountdownTimer_DisplaysInitialTime() {
        // Arrange & Act
        var cut = RenderComponent<CountdownTimer>(parameters => parameters
            .Add(p => p.InitialSeconds, 20));
        
        // Assert
        cut.Find("span.timer-display").TextContent.Should().Be("20");
    }
}
```

2. **Run test to fail:**
```bash
dotnet test frontend/tests/Po.PoDropSquare.Blazor.Tests/
# ❌ Fails - component doesn't exist
```

3. **Implement component:**
```razor
@* File: frontend/src/Po.PoDropSquare.Blazor/Components/CountdownTimer.razor *@
<div class="countdown-timer">
    <span class="timer-display">@RemainingSeconds</span>
</div>

@code {
    [Parameter] public int InitialSeconds { get; set; }
    private int RemainingSeconds { get; set; }
    
    protected override void OnInitialized() {
        RemainingSeconds = InitialSeconds;
    }
}
```

4. **Run test again:**
```bash
dotnet test frontend/tests/Po.PoDropSquare.Blazor.Tests/
# ✅ Passes
```

### Adding Infrastructure Resources

**Use Bicep for all Azure resources:**

```bicep
// File: infra/resources.bicep

// Add a new Azure Function for background processing
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${abbrs.webSitesFunctions}${resourceToken}'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccount.properties.primaryEndpoints.blob
        }
      ]
    }
  }
}
```

**Deploy infrastructure:**
```bash
# Provision and deploy
azd up

# Or just provision infrastructure
azd provision
```

## 📊 Monitoring & Debugging

### Application Insights Queries

The project includes **31 production-ready KQL queries** in `docs/KQL-QUERIES.md`.

**Quick access** - Top 5 queries are in `Program.cs` header comments:

```csharp
// ============================================
// 🔍 KQL QUICK REFERENCE (Application Insights)
// ============================================
// 1. Top 10 Slowest API Requests (last 24h)
//    requests | where timestamp > ago(24h) | summarize AvgDuration = avg(duration) ...
// ... (see Program.cs for full queries)
```

**How to use:**
1. Open Azure Portal → Application Insights
2. Navigate to **Logs** (under Monitoring)
3. Copy/paste query from `docs/KQL-QUERIES.md`
4. Click **Run**

**Example - Find errors in last hour:**
```kql
exceptions
| where timestamp > ago(1h)
| summarize Count = count() by problemId, outerMessage
| order by Count desc
```

### Local Debugging

**Serilog structured logging is configured:**

```csharp
// ✅ Good - Structured logging
_logger.LogInformation("Score submitted: {PlayerName} scored {Score}", playerName, score);

// ❌ Bad - String concatenation
_logger.LogInformation($"Score submitted: {playerName} scored {score}");
```

**Log locations:**
- **Console**: Real-time logs in terminal
- **Files**: `backend/src/Po.PoDropSquare.Api/logs/log-YYYYMMDD.txt`

**Viewing logs:**
```powershell
# Tail logs in real-time
Get-Content backend/src/Po.PoDropSquare.Api/logs/log-20250616.txt -Wait

# Search for errors
Select-String -Path backend/src/Po.PoDropSquare.Api/logs/*.txt -Pattern "Error"
```

### Health Checks

**Health endpoint:** `GET /api/health`

**Custom health checks:**
- Azure Table Storage connectivity
- Application Insights telemetry
- (Add more in `HealthChecks/` folder)

**Testing health check:**
```bash
# Local
curl http://localhost:5000/api/health

# Production
curl https://your-app.azurewebsites.net/api/health
```

## 🚀 Deployment

### Local → Azure Workflow

```bash
# 1. Develop locally
dotnet run --project backend/src/Po.PoDropSquare.Api

# 2. Write tests
dotnet test

# 3. Commit changes
git add .
git commit -m "Add player statistics endpoint"
git push

# 4. GitHub Actions automatically:
#    - Builds solution
#    - Runs tests
#    - Provisions Azure resources (Bicep)
#    - Deploys to App Service
#    - Runs E2E tests
#    - Verifies health check

# 5. Monitor deployment
gh run list
gh run view <run-id>

# 6. Verify live app
curl https://your-app.azurewebsites.net/api/health
```

### Manual Deployment (azd)

```bash
# Login to Azure
azd auth login

# First deployment (provision + deploy)
azd up

# Subsequent deployments (code only)
azd deploy

# View deployment logs
azd monitor --logs
```

## 🛡️ Security & Best Practices

### Rate Limiting

**Configured in `Middleware/RateLimitingMiddleware.cs`:**
- 100 requests per minute per IP
- Returns `429 Too Many Requests` when exceeded

### Input Validation

**Always validate user input:**

```csharp
// ✅ Good - Validation with error handling
[HttpPost]
public async Task<IActionResult> SubmitScore([FromBody] ScoreSubmission submission) {
    if (string.IsNullOrWhiteSpace(submission.PlayerName)) {
        return BadRequest(new ProblemDetails {
            Title = "Invalid player name",
            Detail = "Player name cannot be empty"
        });
    }
    
    if (submission.Score < 0) {
        return BadRequest(new ProblemDetails {
            Title = "Invalid score",
            Detail = "Score must be non-negative"
        });
    }
    
    // Process valid submission
}

// ❌ Bad - No validation
[HttpPost]
public async Task<IActionResult> SubmitScore([FromBody] ScoreSubmission submission) {
    await _service.SaveScoreAsync(submission);  // May crash on null/invalid data
}
```

### Error Handling

**Global error handling middleware** in `Middleware/ErrorHandlingMiddleware.cs`:
- Catches all unhandled exceptions
- Returns RFC 7807 Problem Details
- Logs errors with Serilog
- **Returns raw exception messages** for easier debugging

```json
// Example error response
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Object reference not set to an instance of an object.",
  "traceId": "00-abc123-def456-00"
}
```

### Secrets Management

**❌ Never commit secrets to git:**
- Connection strings
- API keys
- Passwords

**✅ Use these instead:**

| Environment | Method |
|-------------|--------|
| **Local** | `appsettings.Development.json` (git-ignored) |
| **Azure** | App Service Configuration → Application Settings |
| **CI/CD** | GitHub Secrets (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID) |

**Example - Local development:**
```json
// File: appsettings.Development.json (git-ignored)
{
  "ConnectionStrings": {
    "AzureTableStorage": "UseDevelopmentStorage=true"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=local-dev-key"
  }
}
```

## 📝 Code Review Checklist

Before submitting a PR, verify:

- [ ] **Tests written first** (TDD - Red/Green/Refactor)
- [ ] **All tests pass** (`dotnet test` shows 100% pass rate)
- [ ] **Build succeeds** (`dotnet build` with 0 warnings)
- [ ] **Code formatted** (automatic with dotnet format)
- [ ] **Logging added** (Use Serilog structured logging)
- [ ] **Error handling** (Try/catch with meaningful messages)
- [ ] **Documentation updated** (README, API comments, PRD if needed)
- [ ] **No secrets committed** (Check `.gitignore` coverage)
- [ ] **Performance considered** (60 FPS target, <200ms API responses)
- [ ] **Accessibility** (Semantic HTML, ARIA labels for Blazor components)

## 🔄 Common Workflows

### Workflow: Add New Game Feature

**Example: Add "Undo Last Move" feature**

1. **Check STEPS.MD** - Is this the next planned feature?

2. **Write integration test:**
```csharp
[Fact]
public async Task UndoLastMove_RemovesLastBlock_RestoresScore() {
    // Arrange
    var game = new GameState();
    game.AddBlock(new Block { X = 10, Y = 10 });
    var scoreBefore = game.Score;
    
    // Act
    game.UndoLastMove();
    
    // Assert
    Assert.Equal(0, game.Blocks.Count);
    Assert.Equal(0, game.Score);  // Score should reset
}
```

3. **Run test** → ❌ Fails

4. **Implement feature:**
```csharp
public class GameState {
    private Stack<Block> _moveHistory = new();
    
    public void AddBlock(Block block) {
        Blocks.Add(block);
        _moveHistory.Push(block);
        RecalculateScore();
    }
    
    public void UndoLastMove() {
        if (_moveHistory.Count > 0) {
            var lastBlock = _moveHistory.Pop();
            Blocks.Remove(lastBlock);
            RecalculateScore();
        }
    }
}
```

5. **Run test** → ✅ Passes

6. **Add UI button:**
```razor
<button @onclick="UndoLastMove" disabled="@(!CanUndo)">
    Undo Last Move
</button>

@code {
    private bool CanUndo => gameState.MoveHistory.Count > 0;
    
    private void UndoLastMove() {
        gameState.UndoLastMove();
        StateHasChanged();
    }
}
```

7. **Test manually** - Run app, verify button works

8. **Add E2E test:**
```csharp
[Test]
public async Task UndoButton_RemovesLastBlock() {
    await Page.GotoAsync("http://localhost:5000/game");
    await Page.ClickAsync("button:text('Drop Block')");
    await Page.ClickAsync("button:text('Undo Last Move')");
    
    var blockCount = await Page.Locator(".game-block").CountAsync();
    Assert.AreEqual(0, blockCount);
}
```

9. **Mark complete in STEPS.MD**

10. **Commit and push:**
```bash
git add .
git commit -m "Add undo last move feature with tests"
git push
```

### Workflow: Fix a Bug

1. **Reproduce bug** - Write failing test that demonstrates the issue

2. **Run test** → ❌ Fails (confirms bug exists)

3. **Fix the bug** - Minimal code change

4. **Run test** → ✅ Passes (confirms fix works)

5. **Run all tests** - Ensure no regressions

6. **Commit with descriptive message:**
```bash
git commit -m "Fix: Score calculation overflow for scores > Int32.MaxValue

- Changed Score property from int to long
- Updated database schema to bigint
- Added test case for large scores
- Fixes #123"
```

### Workflow: Update Dependencies

```bash
# Check for outdated packages
dotnet list package --outdated

# Update all packages in a project
dotnet add backend/src/Po.PoDropSquare.Api package Microsoft.AspNetCore.OpenApi

# Build to verify compatibility
dotnet build

# Run tests to catch breaking changes
dotnet test

# If all pass, commit
git add .
git commit -m "Update Microsoft.AspNetCore.OpenApi to 9.0.11"
```

## 🆘 Troubleshooting

### Build Errors

**Error:** `The type or namespace name 'Azure' could not be found`

**Solution:**
```bash
dotnet add package Azure.Data.Tables
dotnet build
```

---

**Error:** `Target runtime 'net8.0' is not compatible`

**Solution:**
```xml
<!-- Update .csproj to .NET 9.0 -->
<TargetFramework>net9.0</TargetFramework>
```

### Test Failures

**Error:** E2E tests fail with "Connection refused"

**Solution:** E2E tests require running app
```bash
# Don't run E2E tests locally - they run in CI/CD after deployment
dotnet test --filter "FullyQualifiedName!~E2E"
```

---

**Error:** Integration tests fail with Azure Table Storage error

**Solution:** Start Azurite emulator
```powershell
azurite --silent --location c:\azurite
```

### Deployment Issues

**Error:** `azd up` fails with authentication error

**Solution:**
```bash
# Re-authenticate
azd auth login

# Verify subscription
az account show
az account set --subscription "Your Subscription Name"
```

---

**Error:** GitHub Actions deployment fails

**Solution:** Check OIDC setup in `.github/CICD-SETUP.md`
```bash
# Verify GitHub secrets exist
gh secret list

# Required secrets:
# - AZURE_CLIENT_ID
# - AZURE_TENANT_ID  
# - AZURE_SUBSCRIPTION_ID
```

## 📚 Additional Resources

| Resource | Location | Purpose |
|----------|----------|---------|
| **Product Requirements** | `PRD.MD` | Complete product specification |
| **Implementation Steps** | `STEPS.MD` | 10 high-level development steps |
| **Coding Rules** | `.github/copilot-instructions.md` | Development guidelines |
| **API Tests** | `PoDropSquare.http` | 50+ manual REST client tests |
| **KQL Queries** | `docs/KQL-QUERIES.md` | 31 Application Insights queries |
| **Telemetry Setup** | `docs/APPLICATION-INSIGHTS-SETUP.md` | Monitoring configuration |
| **CI/CD Setup** | `.github/CICD-SETUP.md` | GitHub Actions configuration |
| **Architecture Diagrams** | (Coming in Phase 6) | Mermaid diagrams |

## 🎓 Learning Path for New AI Agents

**Recommended reading order:**

1. **Start here**: `README.md` - Project overview and quick start
2. **Understand product**: `PRD.MD` - What we're building and why
3. **Development rules**: `.github/copilot-instructions.md` - How to build it
4. **This guide**: `AGENTS.MD` - Best practices and workflows
5. **API contracts**: `PoDropSquare.http` - How the API works
6. **Monitoring**: `docs/KQL-QUERIES.md` - How to observe production

**Then explore the code:**
1. `backend/src/Po.PoDropSquare.Core/` - Domain models (start here)
2. `backend/src/Po.PoDropSquare.Api/Controllers/` - API endpoints
3. `backend/src/Po.PoDropSquare.Services/` - Business logic
4. `backend/tests/` - Test patterns and examples
5. `frontend/src/Po.PoDropSquare.Blazor/Pages/` - UI pages

## 🤖 Agent-Specific Tips

### For Code Generation Agents

- **Always read** `.github/copilot-instructions.md` before generating code
- **Follow TDD** - Write tests first, then implementation
- **Use CLI tools** - Prefer `dotnet add package` over manual edits
- **Check STEPS.MD** - Align with project roadmap
- **Run tests** - Verify `dotnet test` passes before moving on

### For Code Review Agents

- **Verify tests exist** - Every feature needs tests
- **Check for logging** - Serilog structured logging required
- **Validate error handling** - Global middleware + local try/catch
- **Look for secrets** - No connection strings in code
- **Assess performance** - 60 FPS game, <200ms API target

### For Debugging Agents

- **Check logs first** - `backend/src/Po.PoDropSquare.Api/logs/`
- **Use KQL queries** - `docs/KQL-QUERIES.md` for production issues
- **Run health check** - `curl http://localhost:5000/api/health`
- **Reproduce with test** - Write failing test to demonstrate bug
- **Check Azure Portal** - Application Insights for live telemetry

### For Documentation Agents

- **Keep README current** - Update when adding features
- **Document API changes** - Update Swagger comments + `.http` file
- **Update PRD** - Major features need PRD updates
- **Add KQL queries** - New metrics need monitoring queries
- **Mermaid diagrams** - Visual documentation for architecture

---

**Built with ❤️ by AI agents following TDD and Clean Architecture principles**
