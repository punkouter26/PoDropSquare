# PoDropSquare - Architecture Diagrams

> Mermaid diagrams documenting the system architecture, data flow, and component relationships

## 📊 Table of Contents

1. [System Architecture](#system-architecture)
2. [Project Dependency Graph](#project-dependency-graph)
3. [Class Diagram - Core Domain](#class-diagram---core-domain)
4. [Sequence Diagram - Score Submission](#sequence-diagram---score-submission)
5. [Deployment Architecture](#deployment-architecture)
6. [Component Hierarchy - Blazor UI](#component-hierarchy---blazor-ui)
7. [CI/CD Pipeline Flow](#cicd-pipeline-flow)
8. [Monitoring & Observability](#monitoring--observability)

---

## System Architecture

High-level overview of the PoDropSquare application architecture:

```mermaid
graph TB
    subgraph "Client Browser"
        UI[Blazor WebAssembly UI]
        Physics[Matter.js Physics Engine]
        LocalStorage[Browser Local Storage]
    end
    
    subgraph "Azure App Service"
        API[ASP.NET Core API]
        Middleware[Error Handling & Rate Limiting]
        Controllers[API Controllers]
        Services[Business Services]
        Repo[Repository Layer]
    end
    
    subgraph "Azure Infrastructure"
        Storage[Azure Table Storage<br/>Leaderboard Data]
        AppInsights[Application Insights<br/>Telemetry]
        LogAnalytics[Log Analytics<br/>Centralized Logs]
    end
    
    subgraph "CI/CD"
        GitHub[GitHub Repository]
        Actions[GitHub Actions<br/>OIDC Auth]
    end
    
    UI -->|HTTP/JSON| API
    UI -->|Physics Simulation| Physics
    UI -->|Offline Scores| LocalStorage
    
    API --> Middleware
    Middleware --> Controllers
    Controllers --> Services
    Services --> Repo
    
    Repo -->|Azure SDK| Storage
    API -->|Telemetry| AppInsights
    API -->|Serilog| LogAnalytics
    
    GitHub -->|Push Event| Actions
    Actions -->|Bicep Deploy| Storage
    Actions -->|Deploy| API
    Actions -->|Configure| AppInsights
    
    style UI fill:#512BD4,stroke:#fff,color:#fff
    style API fill:#0078D4,stroke:#fff,color:#fff
    style Storage fill:#00BCF2,stroke:#fff,color:#fff
    style AppInsights fill:#FF6C37,stroke:#fff,color:#fff
```

**Key Components:**
- **Client Browser**: Blazor WebAssembly SPA with Matter.js physics engine
- **Azure App Service**: ASP.NET Core API hosting both API and static Blazor files
- **Azure Table Storage**: NoSQL database for leaderboard persistence
- **Application Insights**: Real-time telemetry and monitoring
- **GitHub Actions**: Automated CI/CD with OIDC authentication

---

## Project Dependency Graph

Dependency relationships between projects in the solution:

```mermaid
graph TD
    Api[Po.PoDropSquare.Api<br/>.NET 9.0]
    Blazor[Po.PoDropSquare.Blazor<br/>Blazor WASM]
    Core[Po.PoDropSquare.Core<br/>Domain Models]
    Data[Po.PoDropSquare.Data<br/>Table Storage]
    Services[Po.PoDropSquare.Services<br/>Business Logic]
    
    ApiTests[Po.PoDropSquare.Api.Tests<br/>xUnit Integration]
    CoreTests[Po.PoDropSquare.Core.Tests<br/>xUnit Unit]
    E2ETests[Po.PoDropSquare.E2E.Tests<br/>Playwright]
    BlazorTests[Po.PoDropSquare.Blazor.Tests<br/>bUnit]
    
    Api -->|References| Services
    Api -->|References| Core
    Api -->|Hosts Static Files| Blazor
    
    Services -->|References| Core
    Services -->|References| Data
    
    Data -->|References| Core
    
    Blazor -->|References| Core
    
    ApiTests -->|Tests| Api
    CoreTests -->|Tests| Core
    E2ETests -->|Tests| Api
    E2ETests -->|Tests| Blazor
    BlazorTests -->|Tests| Blazor
    
    style Core fill:#90EE90,stroke:#333,color:#000
    style Api fill:#87CEEB,stroke:#333,color:#000
    style Blazor fill:#FFB6C1,stroke:#333,color:#000
    style ApiTests fill:#FFD700,stroke:#333,color:#000
```

**Dependency Rules:**
1. **Core** has zero external dependencies (pure domain)
2. **Data** depends only on Core
3. **Services** depends on Core and Data
4. **Api** depends on Services and Core (entry point)
5. **Blazor** depends only on Core (for DTOs)
6. **All tests** reference the project they test

---

## Class Diagram - Core Domain

Core domain entities and contracts:

```mermaid
classDiagram
    class ScoreEntry {
        +string PartitionKey
        +string RowKey
        +string PlayerName
        +int Score
        +DateTime SubmittedAt
        +string GameMode
        +int BlocksPlaced
        +double SurvivalTime
        +ETag ETag
        +DateTimeOffset? Timestamp
    }
    
    class LeaderboardEntry {
        +string PlayerName
        +int Score
        +DateTime SubmittedAt
        +string GameMode
        +int Rank
    }
    
    class ScoreSubmission {
        +string PlayerName
        +int Score
        +string GameMode
        +int BlocksPlaced
        +double SurvivalTime
    }
    
    class PlayerStats {
        +string PlayerName
        +int GamesPlayed
        +int HighScore
        +double AverageScore
        +int TotalBlocksPlaced
        +double TotalSurvivalTime
        +DateTime LastPlayed
    }
    
    class ApiContracts {
        <<static>>
        +ScoreSubmission
        +LeaderboardEntry
        +PlayerStats
    }
    
    class IScoreRepository {
        <<interface>>
        +SaveScoreAsync(scoreEntry) Task
        +GetTop10Async() Task~List~LeaderboardEntry~~
        +GetLeaderboardAsync(startRank, count) Task~List~LeaderboardEntry~~
        +GetPlayerScoresAsync(playerName) Task~List~ScoreEntry~~
        +GetPlayerRankAsync(playerName) Task~int?~
    }
    
    class IScoreService {
        <<interface>>
        +SubmitScoreAsync(submission) Task~ScoreEntry~
        +GetTop10Async() Task~List~LeaderboardEntry~~
        +GetPlayerStatsAsync(playerName) Task~PlayerStats~
        +ValidateSubmission(submission) bool
    }
    
    ScoreEntry --|> ITableEntity
    ApiContracts ..> ScoreSubmission : uses
    ApiContracts ..> LeaderboardEntry : uses
    ApiContracts ..> PlayerStats : uses
    IScoreRepository ..> ScoreEntry : manages
    IScoreRepository ..> LeaderboardEntry : returns
    IScoreService ..> ScoreSubmission : accepts
    IScoreService ..> LeaderboardEntry : returns
    IScoreService ..> PlayerStats : returns
    
    style ScoreEntry fill:#90EE90,stroke:#333
    style LeaderboardEntry fill:#90EE90,stroke:#333
    style IScoreRepository fill:#FFB6C1,stroke:#333
    style IScoreService fill:#87CEEB,stroke:#333
```

**Key Entities:**
- **ScoreEntry**: Database entity (Azure Table Storage row)
- **LeaderboardEntry**: Read-optimized DTO for leaderboard display
- **ScoreSubmission**: Write-optimized DTO for score submission
- **PlayerStats**: Aggregated statistics for a player

---

## Sequence Diagram - Score Submission

Complete flow of a score submission from browser to database:

```mermaid
sequenceDiagram
    actor Player
    participant UI as Blazor UI
    participant LocalStorage as Browser Storage
    participant API as API Controller
    participant Middleware as Error Handler
    participant Service as Score Service
    participant Repo as Score Repository
    participant Storage as Azure Table Storage
    participant AppInsights as Application Insights
    
    Player->>UI: Submits score
    UI->>UI: Validate input
    
    alt Offline Mode
        UI->>LocalStorage: Save score locally
        UI-->>Player: Show offline confirmation
    else Online Mode
        UI->>API: POST /api/scores<br/>{PlayerName, Score, GameMode}
        API->>Middleware: Handle request
        Middleware->>API: Validate rate limit
        API->>Service: SubmitScoreAsync(submission)
        
        Service->>Service: Validate submission<br/>(name length, score >= 0)
        
        alt Invalid Submission
            Service-->>API: Throw ValidationException
            API-->>UI: 400 Bad Request
            UI-->>Player: Show error message
        else Valid Submission
            Service->>Service: Create ScoreEntry<br/>(PartitionKey, RowKey, Timestamp)
            Service->>Repo: SaveScoreAsync(scoreEntry)
            Repo->>Storage: UpsertEntityAsync(scoreEntry)
            Storage-->>Repo: Success
            Repo-->>Service: Success
            Service-->>API: Return ScoreEntry
            
            API->>AppInsights: Track custom event<br/>"ScoreSubmitted"
            
            API-->>UI: 200 OK + ScoreEntry
            UI->>UI: Update leaderboard
            UI-->>Player: Show success + rank
        end
    end
    
    Note over API,AppInsights: All requests logged via Serilog
    Note over Storage: Data persisted with ETag for concurrency
```

**Error Scenarios:**
1. **Validation Failure**: Returns `400 Bad Request` with Problem Details
2. **Rate Limit Exceeded**: Returns `429 Too Many Requests`
3. **Storage Error**: Returns `500 Internal Server Error` + logs exception
4. **Offline**: Saves to local storage, syncs when back online

---

## Deployment Architecture

Azure infrastructure and deployment flow:

```mermaid
graph TB
    subgraph "GitHub"
        Code[Source Code]
        Actions[GitHub Actions<br/>Workflow]
        Secrets[GitHub Secrets<br/>OIDC Config]
    end
    
    subgraph "Azure Subscription"
        subgraph "Resource Group: rg-podropsquare"
            AppService[App Service<br/>F1 Free Tier<br/>Hosts API + Blazor]
            Storage[Storage Account<br/>Standard_LRS<br/>Table: PoPoDropSquareScores]
            LogAnalytics[Log Analytics Workspace<br/>30-day retention]
            AppInsights[Application Insights<br/>Linked to Log Analytics]
        end
        
        EntraID[Microsoft Entra ID<br/>Service Principal<br/>Federated Credentials]
    end
    
    Code -->|git push| Actions
    Actions -->|OIDC Auth| EntraID
    EntraID -->|Token| Actions
    Actions -->|Read| Secrets
    
    Actions -->|1. Bicep Deploy| Storage
    Actions -->|2. Bicep Deploy| LogAnalytics
    Actions -->|3. Bicep Deploy| AppInsights
    Actions -->|4. Bicep Deploy| AppService
    Actions -->|5. Deploy Code| AppService
    
    AppService -->|Read/Write| Storage
    AppService -->|Send Telemetry| AppInsights
    AppInsights -->|Store Metrics| LogAnalytics
    
    style Actions fill:#2088FF,stroke:#fff,color:#fff
    style AppService fill:#0078D4,stroke:#fff,color:#fff
    style Storage fill:#00BCF2,stroke:#fff,color:#fff
    style AppInsights fill:#FF6C37,stroke:#fff,color:#fff
    style EntraID fill:#00A4EF,stroke:#fff,color:#fff
```

**Deployment Steps:**
1. **Developer pushes code** to GitHub
2. **GitHub Actions triggered** by push event
3. **OIDC authentication** with Azure (no secrets!)
4. **Bicep deployment** provisions/updates infrastructure
5. **Code deployment** publishes to App Service
6. **Health check** verifies `/api/health` endpoint
7. **E2E tests** run against live deployment

**Cost Optimization:**
- **F1 App Service**: Free tier (1 GB RAM, 60 min/day CPU)
- **Storage**: Pay-per-use (~$0.05/GB/month)
- **Application Insights**: Free tier (5 GB/month)
- **Total**: ~$10-20/month

---

## Component Hierarchy - Blazor UI

Blazor WebAssembly component structure:

```mermaid
graph TD
    App[App.razor<br/>Root Component]
    Router[Router]
    MainLayout[MainLayout.razor]
    
    Home[Home.razor<br/>Landing Page]
    Game[Game.razor<br/>Main Gameplay]
    HighScores[HighScores.razor<br/>Leaderboard]
    Diagnostics[Diagnostics.razor<br/>Admin/Debug]
    
    GameCanvas[GameCanvas.razor<br/>HTML5 Canvas + Matter.js]
    Timer[CountdownTimer.razor<br/>20s + 2s Timers]
    ScoreDisplay[ScoreDisplay.razor<br/>Current Score]
    BlockQueue[BlockQueue.razor<br/>Next 3 Blocks]
    
    LeaderboardTable[LeaderboardTable.razor<br/>Top 100 Scores]
    PlayerCard[PlayerCard.razor<br/>Player Stats]
    
    LogViewer[LogViewer.razor<br/>Client Logs]
    HealthStatus[HealthStatus.razor<br/>API Health]
    
    App --> Router
    Router --> MainLayout
    
    MainLayout --> Home
    MainLayout --> Game
    MainLayout --> HighScores
    MainLayout --> Diagnostics
    
    Game --> GameCanvas
    Game --> Timer
    Game --> ScoreDisplay
    Game --> BlockQueue
    
    HighScores --> LeaderboardTable
    HighScores --> PlayerCard
    
    Diagnostics --> LogViewer
    Diagnostics --> HealthStatus
    
    style App fill:#512BD4,stroke:#fff,color:#fff
    style Game fill:#FF6C37,stroke:#fff,color:#fff
    style GameCanvas fill:#00BCF2,stroke:#fff,color:#fff
```

**Component Responsibilities:**
- **App.razor**: Root component, routing configuration
- **MainLayout.razor**: Navigation menu, header, footer
- **Game.razor**: Main game page, orchestrates gameplay components
- **GameCanvas.razor**: Renders physics simulation via Matter.js interop
- **Timer.razor**: Manages dual countdown timers (20s + 2s)
- **ScoreDisplay.razor**: Real-time score updates
- **LeaderboardTable.razor**: Paginated, sortable leaderboard
- **Diagnostics.razor**: Admin page for logs and health checks

---

## CI/CD Pipeline Flow

GitHub Actions workflow stages:

```mermaid
graph LR
    subgraph "Trigger"
        Push[git push<br/>main branch]
    end
    
    subgraph "Build Job"
        Checkout[Checkout Code]
        DotnetSetup[Setup .NET 9.0]
        Restore[dotnet restore]
        Build[dotnet build]
        Test[dotnet test<br/>48+ tests]
        Publish[dotnet publish]
    end
    
    subgraph "Deploy Job"
        AzdLogin[azd auth login<br/>OIDC]
        AzdProvision[azd provision<br/>Bicep]
        AzdDeploy[azd deploy<br/>App Service]
        HealthCheck[curl /api/health]
    end
    
    subgraph "E2E Test Job"
        PlaywrightSetup[Install Playwright]
        E2ETests[Run E2E Tests<br/>11 tests]
        Screenshots[Upload Screenshots]
    end
    
    Push --> Checkout
    Checkout --> DotnetSetup
    DotnetSetup --> Restore
    Restore --> Build
    Build --> Test
    Test --> Publish
    
    Publish --> AzdLogin
    AzdLogin --> AzdProvision
    AzdProvision --> AzdDeploy
    AzdDeploy --> HealthCheck
    
    HealthCheck --> PlaywrightSetup
    PlaywrightSetup --> E2ETests
    E2ETests --> Screenshots
    
    style Push fill:#2088FF,stroke:#fff,color:#fff
    style Test fill:#28A745,stroke:#fff,color:#fff
    style AzdDeploy fill:#0078D4,stroke:#fff,color:#fff
    style E2ETests fill:#FF6C37,stroke:#fff,color:#fff
```

**Pipeline Stages:**

1. **Build** (~2 min)
   - Restore NuGet packages
   - Compile all 8 projects
   - Run 48+ unit/integration tests
   - Publish artifacts

2. **Deploy** (~5 min)
   - Authenticate with Azure (OIDC)
   - Provision infrastructure (Bicep)
   - Deploy API + Blazor to App Service
   - Verify health endpoint

3. **E2E Test** (~3 min)
   - Install Playwright browsers
   - Run 11 end-to-end tests
   - Capture screenshots on failure
   - Upload artifacts

**Security:** Uses OIDC federated credentials - no long-lived secrets!

---

## Monitoring & Observability

Telemetry and logging architecture:

```mermaid
graph TB
    subgraph "Application"
        API[ASP.NET Core API<br/>Serilog]
        Blazor[Blazor WASM<br/>JavaScript Errors]
    end
    
    subgraph "Logging Sinks"
        Console[Console Sink<br/>Development]
        File[File Sink<br/>Rolling Logs]
        AppInsightsSink[App Insights Sink<br/>Production]
    end
    
    subgraph "Azure Monitoring"
        AppInsights[Application Insights]
        LogAnalytics[Log Analytics<br/>Workspace]
        
        subgraph "Query Categories"
            UserActivity[User Activity<br/>4 queries]
            Performance[Performance<br/>8 queries]
            Errors[Errors & Exceptions<br/>7 queries]
            GameMetrics[Game Metrics<br/>3 queries]
            Health[Health Checks<br/>4 queries]
            Business[Business Metrics<br/>4 queries]
            Diagnostics[Diagnostics<br/>4 queries]
        end
        
        Alerts[Alert Rules<br/>3 critical alerts]
        Dashboards[Dashboards<br/>2 templates]
    end
    
    API -->|Structured Logs| Console
    API -->|Structured Logs| File
    API -->|Telemetry| AppInsightsSink
    Blazor -->|Client Errors| API
    
    AppInsightsSink --> AppInsights
    AppInsights --> LogAnalytics
    
    LogAnalytics --> UserActivity
    LogAnalytics --> Performance
    LogAnalytics --> Errors
    LogAnalytics --> GameMetrics
    LogAnalytics --> Health
    LogAnalytics --> Business
    LogAnalytics --> Diagnostics
    
    LogAnalytics --> Alerts
    LogAnalytics --> Dashboards
    
    style API fill:#0078D4,stroke:#fff,color:#fff
    style AppInsights fill:#FF6C37,stroke:#fff,color:#fff
    style Alerts fill:#DC3545,stroke:#fff,color:#fff
    style Dashboards fill:#28A745,stroke:#fff,color:#fff
```

**Monitoring Components:**

1. **Serilog** - Structured logging with multiple sinks
   - Console: Real-time development logs
   - File: Rolling file logs (30-day retention)
   - Application Insights: Production telemetry

2. **Application Insights** - Real-time telemetry
   - Request/response tracking
   - Exception logging
   - Custom events (game scores, user actions)
   - Client-side error tracking
   - Performance counters

3. **31 KQL Queries** (see `docs/KQL-QUERIES.md`)
   - User Activity: Active users, sessions, geography
   - Performance: Latency, slow queries, client load times
   - Errors: Exceptions, failures, JS errors
   - Game Metrics: Score submissions, leaderboard
   - Health: Uptime, availability, dependency health
   - Business: DAU, retention, peak times
   - Diagnostics: Duration distribution, throttling

4. **Alerts** (3 critical)
   - Error rate > 5% (last 5 min)
   - API response time > 1000ms (last 5 min)
   - Dependency failures detected

5. **Dashboards** (2 templates)
   - Real-time monitoring (live metrics)
   - Weekly summary (trends, top errors)

---

## Data Flow - Leaderboard Lookup

How leaderboard data flows through the system:

```mermaid
sequenceDiagram
    participant UI as Blazor UI
    participant Cache as Browser Cache
    participant API as API Controller
    participant Service as Score Service
    participant Repo as Repository
    participant Storage as Azure Table Storage
    
    UI->>Cache: Check cached leaderboard
    
    alt Cache Hit (< 60s old)
        Cache-->>UI: Return cached data
    else Cache Miss
        UI->>API: GET /api/scores/leaderboard?startRank=1&count=50
        API->>Service: GetLeaderboardAsync(1, 50)
        Service->>Repo: GetLeaderboardAsync(1, 50)
        
        Repo->>Storage: Query PoPoDropSquareScores<br/>Filter: Score >= threshold<br/>OrderBy: Score DESC<br/>Take: 50
        
        Storage-->>Repo: Return ScoreEntry[]
        
        Repo->>Repo: Convert to LeaderboardEntry[]<br/>Calculate ranks
        
        Repo-->>Service: Return LeaderboardEntry[]
        Service-->>API: Return LeaderboardEntry[]
        API-->>UI: 200 OK + JSON
        
        UI->>Cache: Store in cache (60s TTL)
        UI->>UI: Render leaderboard table
    end
    
    Note over UI,Storage: Pagination: startRank + count parameters
    Note over Repo,Storage: Azure Table Storage sorted by Score (desc)
```

**Optimization Strategies:**
- **Client-side caching**: 60-second TTL reduces API calls
- **Pagination**: Load only visible rows (50 at a time)
- **Indexed queries**: Azure Table Storage partition/row key optimization
- **Minimal DTOs**: LeaderboardEntry vs full ScoreEntry

---

## Technology Stack Overview

Complete technology breakdown:

```mermaid
mindmap
  root((PoDropSquare))
    Frontend
      Blazor WebAssembly
        .NET 9.0
        C# in Browser
      Matter.js
        Physics Engine
        JavaScript Interop
      Radzen Blazor
        UI Components
      HTML5 Canvas
        Rendering
    Backend
      ASP.NET Core API
        .NET 9.0
        REST Endpoints
      Serilog
        Structured Logging
      Swagger/OpenAPI
        API Documentation
      Health Checks
        Custom Checks
    Data
      Azure Table Storage
        NoSQL Database
        Leaderboard
      Azurite
        Local Emulator
      Azure SDK
        Table Client
    Testing
      xUnit
        Unit Tests
        Integration Tests
      Playwright
        E2E Tests
        Chromium
      bUnit
        Component Tests
      WebApplicationFactory
        API Testing
    Infrastructure
      Azure Bicep
        IaC
      Azure Developer CLI
        azd
      App Service
        F1 Free Tier
      Application Insights
        Telemetry
      Log Analytics
        Centralized Logs
    CI/CD
      GitHub Actions
        Build/Deploy
      OIDC Auth
        Federated Credentials
      E2E Tests
        Post-Deploy
```

---

## Summary

These diagrams provide comprehensive visual documentation of the PoDropSquare architecture:

1. **System Architecture** - High-level component overview
2. **Project Dependencies** - Code organization and references
3. **Class Diagram** - Core domain model
4. **Sequence Diagrams** - Request/response flows
5. **Deployment** - Azure infrastructure and CI/CD
6. **Component Hierarchy** - Blazor UI structure
7. **Monitoring** - Observability and telemetry
8. **Data Flow** - End-to-end data processing

**Viewing These Diagrams:**
- **GitHub**: Renders Mermaid natively in markdown
- **VS Code**: Install "Markdown Preview Mermaid Support" extension
- **Browser**: Use [Mermaid Live Editor](https://mermaid.live/)

**Keeping Updated:**
- Update diagrams when adding new components
- Document new API endpoints in sequence diagrams
- Add new Azure resources to deployment diagram
- Keep class diagram in sync with domain models

---

**Built with ❤️ using Mermaid diagrams for visual documentation**
