# PoDropSquare - Physics-Based Tower Building Game

A physics-based puzzle game where players drop colored blocks to build stable towers while racing against dual timers.

## Project Structure

This project follows a clean architecture with separate frontend and backend components:

```
PoDropSquare/
├── backend/
│   ├── src/
│   │   ├── Po.PoDropSquare.Api/          # ASP.NET Core Web API
│   │   ├── Po.PoDropSquare.Core/         # Domain models and business logic
│   │   ├── Po.PoDropSquare.Data/         # Azure Table Storage data access
│   │   └── Po.PoDropSquare.Services/     # Application services
│   └── tests/
│       ├── Po.PoDropSquare.Api.Tests/    # API integration tests
│       ├── Po.PoDropSquare.Core.Tests/   # Unit tests for business logic
│       └── Po.PoDropSquare.E2E.Tests/    # End-to-end tests with Playwright
├── frontend/
│   ├── src/
│   │   └── Po.PoDropSquare.Blazor/       # Blazor WebAssembly application
│   └── tests/
│       └── Po.PoDropSquare.Blazor.Tests/ # Blazor component tests with bUnit
└── specs/
    └── 001-podropsquare-is-a/            # Design specifications and contracts
```

## Technology Stack

### Backend
- **.NET 8** - Application framework
- **ASP.NET Core Web API** - RESTful API endpoints
- **Azure Table Storage** - Leaderboard persistence
- **Azurite** - Local development storage emulator
- **Serilog** - Structured logging
- **xUnit** - Unit and integration testing
- **Playwright** - End-to-end testing

### Frontend
- **Blazor WebAssembly** - Client-side web framework
- **Matter.js** - Physics engine via JavaScript interop
- **bUnit** - Blazor component testing

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js (for Azurite emulator)
- Modern web browser with WebAssembly support

### Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PoDropSquare
   ```

2. **Start Azure Storage Emulator**
   ```powershell
   .\start-azurite.ps1
   ```
   This starts Azurite on port 10002 for Table Storage.

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Start the backend API**
   ```bash
   cd backend/src/Po.PoDropSquare.Api
   dotnet run
   ```
   API will be available at `https://localhost:7001`

6. **Start the frontend application**
   ```bash
   cd frontend/src/Po.PoDropSquare.Blazor
   dotnet run
   ```
   Application will be available at `https://localhost:7000`

## Development Workflow

This project follows Test-Driven Development (TDD) principles:

1. **Red**: Write failing tests first
2. **Green**: Write minimal code to make tests pass
3. **Refactor**: Improve code while keeping tests green

### Running Tests by Category

```bash
# Unit tests
dotnet test backend/tests/Po.PoDropSquare.Core.Tests/

# API integration tests
dotnet test backend/tests/Po.PoDropSquare.Api.Tests/

# Blazor component tests
dotnet test frontend/tests/Po.PoDropSquare.Blazor.Tests/

# End-to-end tests
dotnet test backend/tests/Po.PoDropSquare.E2E.Tests/
```

## Configuration

### Azure Table Storage Connection
- **Development**: Uses Azurite emulator (configured in `appsettings.Development.json`)
- **Production**: Configure `ConnectionStrings:AzureTableStorage` in production settings

### Logging
- **Console**: Structured JSON logs for development
- **File**: Rolling file logs in `logs/` directory
- **Configuration**: Serilog settings in `appsettings.json`

## API Endpoints

The backend API provides the following endpoints:

- `POST /api/scores` - Submit game score
- `GET /api/scores/top10` - Retrieve top 10 leaderboard
- `GET /health` - Health check endpoint

## Game Features

- **Physics Simulation**: Real-time block physics using Matter.js
- **Dual Timers**: 20-second survival + 2-second danger countdown
- **Cross-Platform**: Touch and mouse input support
- **Leaderboard**: Persistent score tracking with Azure Table Storage
- **Offline Support**: Local browser storage for offline gameplay

## Performance Targets

- **60 FPS** gameplay
- **<50ms** input response time
- **<3s** initial load time
- **<200ms** API response time
- **99.9%** uptime

## Next Steps

After completing Phase 3.1 (Setup & Infrastructure), the next phases are:

- **Phase 3.2**: Write failing tests (TDD approach)
- **Phase 3.3**: Implement core data models
- **Phase 3.4**: Build data access and services
- **Phase 3.5**: Create API endpoints
- **Phase 3.6**: Develop game components
- **Phase 3.7**: Integrate physics engine
- **Phase 3.8**: Configure integrations
- **Phase 3.9**: Polish and performance optimization

## Contributing

This project follows strict architectural principles:
- **Simplicity**: Direct framework usage, minimal abstractions
- **Testing**: TDD with comprehensive test coverage
- **Observability**: Structured logging throughout
- **Versioning**: Semantic versioning with automated builds

See `/specs/001-podropsquare-is-a/plan.md` for detailed implementation guidance.