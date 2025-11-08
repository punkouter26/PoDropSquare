# AI Coding Agent Guide for PoDropSquare

> Best practices and context for AI coding assistants working with the PoDropSquare codebase

## 🎯 Project Overview

**PoDropSquare** is a physics-based tower building game where players race against dual timers (20s survival + 2s danger countdown) to build stable towers by dropping colored blocks. The application is built with:

- **Frontend**: Blazor WebAssembly with Matter.js physics engine
- **Backend**: ASP.NET Core Web API (.NET 9.0) using **Minimal APIs + CQRS + Vertical Slice Architecture**
- **Data**: Azure Table Storage (leaderboard persistence)
- **Infrastructure**: Azure Bicep, deployed via Azure Developer CLI (azd)
- **CI/CD**: GitHub Actions with OIDC authentication
- **Monitoring**: Application Insights + Serilog structured logging + OpenTelemetry
- **Package Management**: Centralized via `Directory.Packages.props`

## 🏛️ Core Architecture & Design Principles

### 1. Vertical Slice Architecture (VSA)

**All code MUST be organized using Vertical Slice Architecture** - features are organized by business capability, not by technical layer.

```
/src/Po.[AppName].Api/Features/
├── Scores/
│   ├── SubmitScore/
│   │   ├── SubmitScoreCommand.cs      # MediatR command
│   │   ├── SubmitScoreHandler.cs      # Command handler
│   │   ├── SubmitScoreValidator.cs    # FluentValidation
│   │   └── SubmitScoreEndpoint.cs     # Minimal API endpoint
│   ├── GetLeaderboard/
│   │   ├── GetLeaderboardQuery.cs
│   │   ├── GetLeaderboardHandler.cs
│   │   └── GetLeaderboardEndpoint.cs
│   └── GetPlayerRank/
│       ├── GetPlayerRankQuery.cs
│       ├── GetPlayerRankHandler.cs
│       └── GetPlayerRankEndpoint.cs
├── Health/
│   └── HealthCheckEndpoint.cs
└── ...other features
```

**Why Vertical Slices?**
- ✅ Features are self-contained and easy to locate
- ✅ Changes to one feature don't affect others
- ✅ Easy to delete features without breaking the system
- ✅ Team members can work on different features without conflicts

### 2. SOLID Principles & GoF Patterns

**Document where these are applied:**

| Principle/Pattern | Where Applied | Example |
|-------------------|---------------|---------|
| **Single Responsibility** | Each handler, each endpoint | `SubmitScoreHandler` only handles score submission |
| **Open/Closed** | Strategy pattern for scoring | New scoring algorithms extend base interface |
| **Dependency Inversion** | All handlers depend on abstractions | `ITableStorageClient`, `IScoreCalculator` |
| **Command Pattern** | MediatR commands/queries | `SubmitScoreCommand`, `GetLeaderboardQuery` |
| **Repository Pattern** | Data access abstraction | `IScoreRepository` |
| **Mediator Pattern** | MediatR for CQRS | Decouples endpoint from business logic |

### 3. Minimal APIs (Required for All New Endpoints)

**❌ Old approach (Controllers):**
```csharp
[ApiController]
[Route("api/[controller]")]
public class ScoresController : ControllerBase {
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ScoreSubmission request) { }
}
```

**✅ New approach (Minimal APIs):**
```csharp
// File: Features/Scores/SubmitScore/SubmitScoreEndpoint.cs
public static class SubmitScoreEndpoint
{
    public static void MapSubmitScore(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/scores", async (
            SubmitScoreCommand command,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("SubmitScore")
        .WithOpenApi()
        .Produces<ScoreResult>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithTags("Scores");
    }
}
```

### 4. CQRS with MediatR

**Separate Commands (writes) from Queries (reads):**

**Command example:**
```csharp
// File: Features/Scores/SubmitScore/SubmitScoreCommand.cs
public record SubmitScoreCommand(string PlayerName, int Score) : IRequest<ScoreResult>;

// File: Features/Scores/SubmitScore/SubmitScoreHandler.cs
public class SubmitScoreHandler : IRequestHandler<SubmitScoreCommand, ScoreResult>
{
    private readonly IScoreRepository _repository;
    private readonly ILogger<SubmitScoreHandler> _logger;
    
    public SubmitScoreHandler(IScoreRepository repository, ILogger<SubmitScoreHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<ScoreResult> Handle(SubmitScoreCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Submitting score: {PlayerName} scored {Score}", 
            request.PlayerName, request.Score);
            
        var entity = new ScoreEntity(request.PlayerName, request.Score);
        await _repository.SaveAsync(entity, ct);
        
        return new ScoreResult(entity.Id, entity.Rank);
    }
}
```

**Query example:**
```csharp
// File: Features/Scores/GetLeaderboard/GetLeaderboardQuery.cs
public record GetLeaderboardQuery(int Top = 10) : IRequest<List<LeaderboardEntry>>;

// File: Features/Scores/GetLeaderboard/GetLeaderboardHandler.cs
public class GetLeaderboardHandler : IRequestHandler<GetLeaderboardQuery, List<LeaderboardEntry>>
{
    private readonly IScoreRepository _repository;
    
    public GetLeaderboardHandler(IScoreRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<List<LeaderboardEntry>> Handle(GetLeaderboardQuery request, CancellationToken ct)
    {
        return await _repository.GetTopScoresAsync(request.Top, ct);
    }
}
```

### 5. OpenTelemetry for Custom Telemetry

**Use OpenTelemetry abstractions for all custom application telemetry:**

```csharp
// File: Program.cs
var activitySource = new ActivitySource("PoDropSquare.Api");
var meter = new Meter("PoDropSquare.Api");
var scoreCounter = meter.CreateCounter<int>("scores.submitted");

builder.Services.AddSingleton(activitySource);
builder.Services.AddSingleton(scoreCounter);

// File: Features/Scores/SubmitScore/SubmitScoreHandler.cs
public class SubmitScoreHandler : IRequestHandler<SubmitScoreCommand, ScoreResult>
{
    private readonly ActivitySource _activitySource;
    private readonly Counter<int> _scoreCounter;
    
    public async Task<ScoreResult> Handle(SubmitScoreCommand request, CancellationToken ct)
    {
        using var activity = _activitySource.StartActivity("SubmitScore");
        activity?.SetTag("player.name", request.PlayerName);
        activity?.SetTag("score", request.Score);
        
        // Business logic...
        
        _scoreCounter.Add(1, new KeyValuePair<string, object?>("player", request.PlayerName));
        
        return result;
    }
}
```

### 6. Robust Error Handling

**All API calls MUST return robust error details on exceptions:**

```csharp
// File: Middleware/GlobalExceptionMiddleware.cs
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An error occurred",
            Status = StatusCodes.Status500InternalServerError,
            Detail = exception?.Message, // Return raw exception message
            Instance = context.Request.Path
        };
        
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        problemDetails.Extensions["exception"] = exception?.ToString(); // Full stack trace
        
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(problemDetails);
    });
});
```

## � Solution & Project Structure

### Standard Folder Structure

```
/
├── src/                                    # Source code
│   ├── Po.[AppName].Api/                  # Backend API (Minimal APIs + CQRS)
│   │   ├── Features/                      # Vertical slices by feature
│   │   │   ├── Scores/
│   │   │   │   ├── SubmitScore/
│   │   │   │   ├── GetLeaderboard/
│   │   │   │   └── GetPlayerRank/
│   │   │   ├── Health/
│   │   │   └── ...
│   │   ├── Middleware/                    # Global middleware
│   │   ├── Program.cs                     # App startup & configuration
│   │   └── appsettings.json
│   ├── Po.[AppName].Client/               # Blazor WebAssembly frontend
│   │   ├── Pages/
│   │   ├── Components/
│   │   ├── Services/
│   │   └── wwwroot/
│   └── Po.[AppName].Shared/               # DTOs and shared models
│       ├── Commands/
│       ├── Queries/
│       └── DTOs/
├── tests/                                  # All test projects
│   ├── Po.[AppName].UnitTests/            # xUnit unit tests (mocked dependencies)
│   ├── Po.[AppName].IntegrationTests/     # xUnit integration tests (WebApplicationFactory + Azurite)
│   └── Po.[AppName].E2ETests/             # Playwright E2E tests (TypeScript)
├── infra/                                  # Azure infrastructure (Bicep)
│   ├── main.bicep
│   ├── resources.bicep
│   └── main.parameters.json
├── docs/                                   # Documentation
│   ├── PRD.md                             # Product Requirements Document
│   ├── diagrams/                          # Mermaid diagram files
│   │   ├── architecture.mmd
│   │   └── workflows.mmd
│   ├── KQL-QUERIES.md
│   └── APPLICATION-INSIGHTS-SETUP.md
├── scripts/                                # Utility scripts
│   ├── seed-data.ps1
│   ├── deploy-dev.ps1
│   └── run-tests.ps1
├── Directory.Packages.props               # Centralized NuGet package versions
├── global.json                            # .NET SDK version (9.0.xxx)
├── azure.yaml                             # Azure Developer CLI config
└── PoDropSquare.sln                       # Solution file
```

### Key Requirements

1. **User-facing HTML `<title>` tag** must match the `.sln` file name (e.g., "PoDropSquare")
2. **All NuGet packages** use `Directory.Packages.props` for centralized version management
3. **Features folder** contains vertical slices organized by business capability
4. **Shared project** contains DTOs, commands, queries used by both API and Client

## 📋 Guiding Principles

### 1. Automate with CLI Tools

**Prefer single-line CLI commands** over creating one-shot PowerShell scripts for simple tasks.

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

# ✅ Good - Single-line CLI command
dotnet ef database update --project src/Po.PoDropSquare.Api

# ❌ Bad - Creating a one-shot script for this
# Creating update-database.ps1 with one command
```

### 2. Maintain Simplicity
- **Keep files under ~500 lines** - Refactor larger files into focused components
- **Remove unused code** - Delete dead code regularly
- **Avoid over-abstraction** - Use built-in framework features before creating custom abstractions
- **Prefer composition over inheritance** - Use services/components over deep class hierarchies

### 3. Test-Driven Development (TDD)

**Strictly follow the TDD workflow: Red -> Green -> Refactor**

1. **🔴 Red** - Write a failing test
2. **🟢 Green** - Implement minimum code to pass
3. **♻️ Refactor** - Improve while keeping tests green

**Example workflow:**
```bash
# 1. Write failing test
# File: Features/Scores/SubmitScore/SubmitScoreHandlerTests.cs
[Fact]
public async Task Handle_WithValidScore_SavesAndReturnsResult() {
    // Arrange
    var repository = new Mock<IScoreRepository>();
    var handler = new SubmitScoreHandler(repository.Object, _logger);
    var command = new SubmitScoreCommand("Alice", 1500);
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.NotNull(result);  // ❌ FAILS - not implemented yet
    repository.Verify(r => r.SaveAsync(It.IsAny<ScoreEntity>(), It.IsAny<CancellationToken>()), Times.Once);
}

# 2. Run test to see it fail
dotnet test --filter "Handle_WithValidScore_SavesAndReturnsResult"

# 3. Implement minimum code
public async Task<ScoreResult> Handle(SubmitScoreCommand request, CancellationToken ct)
{
    var entity = new ScoreEntity(request.PlayerName, request.Score);
    await _repository.SaveAsync(entity, ct);
    return new ScoreResult(entity.Id, entity.Rank);
}

# 4. Run test again
dotnet test --filter "Handle_WithValidScore_SavesAndReturnsResult"  # ✅ PASSES

# 5. Refactor if needed (add logging, validation, etc.)
public async Task<ScoreResult> Handle(SubmitScoreCommand request, CancellationToken ct)
{
    _logger.LogInformation("Submitting score: {PlayerName} scored {Score}", 
        request.PlayerName, request.Score);
        
    var entity = new ScoreEntity(request.PlayerName, request.Score);
    await _repository.SaveAsync(entity, ct);
    
    return new ScoreResult(entity.Id, entity.Rank);
}

# 6. Verify tests still pass
dotnet test
```

### 4. Environment & Configuration

**Requirements:**
- **Target Framework**: All projects must target `.NET 9.0` (`<TargetFramework>net9.0</TargetFramework>`)
- **SDK Version**: `global.json` specifies `9.0.xxx` SDK version
- **Nullable Reference Types**: Enabled in all `.csproj` files (`<Nullable>enable</Nullable>`)
- **Local Secrets**: Use .NET User Secrets for sensitive data (never commit secrets)
- **Storage**: Azure Table Storage (production) + Azurite (local/testing)

**Example global.json:**
```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
```

### 5. Always Run API Project (Not Blazor Standalone)

The API project **hosts** the Blazor WebAssembly application at ports:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:5001`

**✅ Correct way to run the app:**
```bash
dotnet run --project src/Po.PoDropSquare.Api
# App available at http://localhost:5000
```

**❌ Incorrect - Don't run Blazor standalone:**
```bash
# This won't work - CORS issues, API not available
dotnet run --project src/Po.PoDropSquare.Client
```

## 🏗️ Backend (API) Implementation

### API Documentation

**Enable Swagger/OpenAPI for all endpoints:**

```csharp
// File: Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

**Generate .http files with sample requests** for manual testing:

```http
### Submit Score
POST {{baseUrl}}/api/scores
Content-Type: application/json

{
  "playerName": "Alice",
  "score": 1500
}

### Get Leaderboard
GET {{baseUrl}}/api/scores/leaderboard?top=10
```

### Health Checks

**Implement comprehensive health checks at `/api/health`:**

```csharp
// File: Features/Health/HealthCheckEndpoint.cs
public static class HealthCheckEndpoint
{
    public static void MapHealthCheck(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/api/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
        .WithName("HealthCheck")
        .WithOpenApi()
        .WithTags("Health");
    }
}

// File: Program.cs
builder.Services.AddHealthChecks()
    .AddAzureTableStorage(builder.Configuration["AzureTableStorage:ConnectionString"])
    .AddApplicationInsightsPublisher();
```

### Serilog Configuration

**Use structured logging with Serilog:**

```csharp
// File: Program.cs
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console()
    .WriteTo.Debug()
    .WriteTo.ApplicationInsights(services.GetRequiredService<TelemetryConfiguration>(), 
        TelemetryConverter.Traces));
```

**appsettings.Development.json:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "Debug" }
    ]
  }
}
```

**appsettings.json (Production):**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      { "Name": "ApplicationInsights" }
    ]
  }
}
```

### Error Handling

**Use structured logging in all catch blocks:**

```csharp
public async Task<ScoreResult> Handle(SubmitScoreCommand request, CancellationToken ct)
{
    try
    {
        _logger.LogInformation("Submitting score: {PlayerName} scored {Score}", 
            request.PlayerName, request.Score);
            
        var entity = new ScoreEntity(request.PlayerName, request.Score);
        await _repository.SaveAsync(entity, ct);
        
        return new ScoreResult(entity.Id, entity.Rank);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to submit score for {PlayerName}", request.PlayerName);
        throw;
    }
}
```

## 🎨 Frontend (Client) Implementation

### UI Framework

**Primary component library: Microsoft.FluentUI.AspNetCore.Components**

```razor
@using Microsoft.FluentUI.AspNetCore.Components

<FluentButton Appearance="Appearance.Accent" @onclick="SubmitScore">
    Submit Score
</FluentButton>

<FluentDataGrid Items="@leaderboardEntries">
    <PropertyColumn Property="@(e => e.PlayerName)" Title="Player" />
    <PropertyColumn Property="@(e => e.Score)" Title="Score" />
</FluentDataGrid>
```

**Only use Radzen.Blazor if essential for complex requirements.**

### Responsive Design

**UI must be mobile-first (portrait mode), responsive, fluid, and touch-friendly:**

```css
/* Mobile-first approach */
.game-container {
    width: 100%;
    padding: 1rem;
    touch-action: manipulation;
}

/* Tablet and larger */
@media (min-width: 768px) {
    .game-container {
        max-width: 800px;
        margin: 0 auto;
    }
}

/* Desktop */
@media (min-width: 1024px) {
    .game-container {
        max-width: 1200px;
    }
}
```

### Naming Conventions

- **Projects**: `Po.[AppName].[LayerName]` (e.g., `Po.PoDropSquare.Api`)
- **Features**: PascalCase folder names matching business capability (e.g., `Scores`, `Health`)
- **Commands**: Suffix with `Command` (e.g., `SubmitScoreCommand`)
- **Queries**: Suffix with `Query` (e.g., `GetLeaderboardQuery`)
- **Handlers**: Suffix with `Handler` (e.g., `SubmitScoreHandler`)
- **Endpoints**: Suffix with `Endpoint` (e.g., `SubmitScoreEndpoint`)
- **Tests**: `[ClassName]Tests.cs` (e.g., `SubmitScoreHandlerTests.cs`)

## 🧪 Testing Strategy

### Test Types & Requirements

| Type | Framework | Purpose | Requirements |
|------|-----------|---------|--------------|
| **Unit Tests** | xUnit | Test handlers/services in isolation | Mock all external dependencies |
| **Integration Tests** | xUnit + WebApplicationFactory | Test API endpoints with infrastructure | Use isolated Azurite, generate test data with Bogus |
| **Component Tests** | bUnit | Test Blazor components | Mock IHttpClientFactory, IJSRuntime |
| **E2E Tests** | Playwright (TypeScript) | Test full user workflows | Target Chromium, use network interception, axe-core, screenshots |

### Test Naming Convention

**Pattern: `MethodName_StateUnderTest_ExpectedBehavior`**

```csharp
// Unit Test Examples
[Fact]
public async Task Handle_WithValidScore_SavesAndReturnsResult() { }

[Fact]
public async Task Handle_WithNullPlayerName_ThrowsArgumentException() { }

// Integration Test Examples
[Fact]
public async Task SubmitScore_WithValidData_Returns201Created() { }

[Fact]
public async Task GetLeaderboard_WithTop10_ReturnsOrderedByScoreDescending() { }

// Component Test Examples
[Fact]
public void GameTimer_WhenTimeExpires_TriggersOnTimeUpCallback() { }

[Fact]
public void ScoreDisplay_WithZeroScore_ShowsZero() { }
```

### Unit Tests (xUnit)

**Must cover all new backend business logic (handlers, validators):**

```csharp
// File: tests/Po.[AppName].UnitTests/Features/Scores/SubmitScore/SubmitScoreHandlerTests.cs
public class SubmitScoreHandlerTests
{
    private readonly Mock<IScoreRepository> _repositoryMock;
    private readonly Mock<ILogger<SubmitScoreHandler>> _loggerMock;
    private readonly SubmitScoreHandler _handler;
    
    public SubmitScoreHandlerTests()
    {
        _repositoryMock = new Mock<IScoreRepository>();
        _loggerMock = new Mock<ILogger<SubmitScoreHandler>>();
        _handler = new SubmitScoreHandler(_repositoryMock.Object, _loggerMock.Object);
    }
    
    [Fact]
    public async Task Handle_WithValidScore_SavesAndReturnsResult()
    {
        // Arrange
        var command = new SubmitScoreCommand("Alice", 1500);
        var expectedEntity = new ScoreEntity("Alice", 1500);
        _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<ScoreEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEntity.Id, result.Id);
        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<ScoreEntity>(e => e.PlayerName == "Alice" && e.Score == 1500),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Tests (xUnit + WebApplicationFactory)

**Create a "happy path" test for every new API endpoint:**

```csharp
// File: tests/Po.[AppName].IntegrationTests/Features/Scores/SubmitScoreIntegrationTests.cs
public class SubmitScoreIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly Faker<ScoreSubmission> _scoreFaker;
    
    public SubmitScoreIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Configure Azurite for testing
                services.Configure<AzureStorageOptions>(options =>
                {
                    options.ConnectionString = "UseDevelopmentStorage=true";
                });
            });
        }).CreateClient();
        
        // Use Bogus to generate realistic test data
        _scoreFaker = new Faker<ScoreSubmission>()
            .RuleFor(s => s.PlayerName, f => f.Name.FirstName())
            .RuleFor(s => s.Score, f => f.Random.Int(100, 10000));
    }
    
    [Fact]
    public async Task SubmitScore_WithValidData_Returns201Created()
    {
        // Arrange
        var scoreSubmission = _scoreFaker.Generate();
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/scores", scoreSubmission);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ScoreResult>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
    }
}
```

### Component Tests (bUnit)

**Must cover all new Blazor components:**

```csharp
// File: tests/Po.[AppName].Blazor.Tests/Components/GameTimerTests.cs
public class GameTimerTests : TestContext
{
    [Fact]
    public void GameTimer_WhenInitialized_DisplaysInitialTime()
    {
        // Arrange & Act
        var cut = RenderComponent<GameTimer>(parameters => parameters
            .Add(p => p.InitialSeconds, 20));
        
        // Assert
        cut.Find(".timer-display").TextContent.Should().Be("20");
    }
    
    [Fact]
    public async Task GameTimer_WhenTimeExpires_TriggersCallback()
    {
        // Arrange
        var callbackTriggered = false;
        var cut = RenderComponent<GameTimer>(parameters => parameters
            .Add(p => p.InitialSeconds, 1)
            .Add(p => p.OnTimeUp, () => { callbackTriggered = true; }));
        
        // Act
        await Task.Delay(1100); // Wait for timer
        
        // Assert
        callbackTriggered.Should().BeTrue();
    }
}
```

### E2E Tests (Playwright + TypeScript)

**Write tests in TypeScript, targeting Chromium:**

```typescript
// File: tests/Po.[AppName].E2ETests/gameplay.spec.ts
import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Core Gameplay', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API responses for stable testing
    await page.route('**/api/scores/leaderboard', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { playerName: 'Alice', score: 1500, rank: 1 },
          { playerName: 'Bob', score: 1200, rank: 2 }
        ])
      });
    });
    
    await page.goto('http://localhost:5000/game');
  });

  test('should display game board on load', async ({ page }) => {
    // Arrange & Act
    const gameBoard = page.locator('.game-board');
    
    // Assert
    await expect(gameBoard).toBeVisible();
  });

  test('should drop block when button clicked', async ({ page }) => {
    // Arrange
    const dropButton = page.locator('button:has-text("Drop Block")');
    
    // Act
    await dropButton.click();
    
    // Assert
    const blocks = page.locator('.game-block');
    await expect(blocks).toHaveCount(1);
  });

  test('should pass accessibility checks', async ({ page }) => {
    // Arrange
    const accessibilityScanResults = await new AxeBuilder({ page }).analyze();
    
    // Assert
    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should match visual snapshot', async ({ page }) => {
    // Arrange & Act
    await page.waitForLoadState('networkidle');
    
    // Assert (visual regression)
    await expect(page).toHaveScreenshot('game-initial-state.png');
  });
});

test.describe('Mobile Gameplay', () => {
  test.use({
    viewport: { width: 375, height: 667 }, // iPhone SE
    isMobile: true,
    hasTouch: true
  });

  test('should be touch-friendly on mobile', async ({ page }) => {
    await page.goto('http://localhost:5000/game');
    
    // Tap to drop block
    await page.locator('button:has-text("Drop Block")').tap();
    
    const blocks = page.locator('.game-block');
    await expect(blocks).toHaveCount(1);
  });
});
```

### Writing Good Tests

**✅ Good test - Arrange/Act/Assert pattern:**
```csharp
[Fact]
public async Task GetLeaderboard_WithTop10_ReturnsOrderedByScoreDescending() {
    // Arrange
    var client = _factory.CreateClient();
    await SeedScores(new[] { 100, 500, 300 });
    
    // Act
    var response = await client.GetAsync("/api/scores/leaderboard?top=10");
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
dotnet test tests/Po.PoDropSquare.UnitTests/

# Specific test class
dotnet test --filter "SubmitScoreHandlerTests"

# Specific test method
dotnet test --filter "Handle_WithValidScore_SavesAndReturnsResult"

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# E2E tests (requires running app)
cd tests/Po.PoDropSquare.E2ETests
npx playwright test
npx playwright test --project=chromium
npx playwright test --headed  # Run with visible browser
npx playwright show-report    # View test report
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

**Follow TDD workflow with Vertical Slice Architecture:**

1. **Write integration test first:**
```csharp
// File: tests/Po.[AppName].IntegrationTests/Features/Stats/GetPlayerStatsIntegrationTests.cs
[Fact]
public async Task GetPlayerStats_WithValidPlayer_ReturnsPlayerStatistics() {
    // Arrange
    var client = _factory.CreateClient();
    var faker = new Faker();
    var playerName = faker.Name.FirstName();
    await SeedPlayerData(playerName, new[] { 100, 200, 300 });
    
    // Act
    var response = await client.GetAsync($"/api/stats/{playerName}");
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var stats = await response.Content.ReadFromJsonAsync<PlayerStats>();
    Assert.NotNull(stats);
    Assert.Equal(3, stats.GamesPlayed);
    Assert.Equal(200, stats.AverageScore);
}
```

2. **Run test to see it fail:**
```bash
dotnet test --filter "GetPlayerStats_WithValidPlayer_ReturnsPlayerStatistics"
# ❌ Fails - endpoint doesn't exist yet
```

3. **Create the vertical slice (Query + Handler + Endpoint):**

```csharp
// File: src/Po.[AppName].Api/Features/Stats/GetPlayerStats/GetPlayerStatsQuery.cs
public record GetPlayerStatsQuery(string PlayerName) : IRequest<PlayerStats>;

// File: src/Po.[AppName].Api/Features/Stats/GetPlayerStats/GetPlayerStatsHandler.cs
public class GetPlayerStatsHandler : IRequestHandler<GetPlayerStatsQuery, PlayerStats>
{
    private readonly IScoreRepository _repository;
    private readonly ILogger<GetPlayerStatsHandler> _logger;
    
    public GetPlayerStatsHandler(IScoreRepository repository, ILogger<GetPlayerStatsHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<PlayerStats> Handle(GetPlayerStatsQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Getting stats for player: {PlayerName}", request.PlayerName);
        
        var scores = await _repository.GetPlayerScoresAsync(request.PlayerName, ct);
        
        return new PlayerStats
        {
            PlayerName = request.PlayerName,
            GamesPlayed = scores.Count,
            AverageScore = scores.Any() ? (int)scores.Average(s => s.Score) : 0,
            HighScore = scores.Any() ? scores.Max(s => s.Score) : 0
        };
    }
}

// File: src/Po.[AppName].Api/Features/Stats/GetPlayerStats/GetPlayerStatsEndpoint.cs
public static class GetPlayerStatsEndpoint
{
    public static void MapGetPlayerStats(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stats/{playerName}", async (
            string playerName,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetPlayerStatsQuery(playerName);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetPlayerStats")
        .WithOpenApi()
        .Produces<PlayerStats>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("Stats");
    }
}
```

4. **Register the endpoint in Program.cs:**
```csharp
// File: Program.cs
app.MapGetPlayerStats();
```

5. **Run test again:**
```bash
dotnet test --filter "GetPlayerStats_WithValidPlayer_ReturnsPlayerStatistics"
# ✅ Passes
```

6. **Add to .http file for manual testing:**
```http
### Get Player Statistics
GET {{baseUrl}}/api/stats/Alice
Accept: application/json

### Get Player Statistics - Not Found
GET {{baseUrl}}/api/stats/NonExistentPlayer
Accept: application/json
```

7. **Build and verify all tests pass:**
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
