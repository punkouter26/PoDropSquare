# Tasks: PoDropSquare Physics-Based Tower Building Game

**Input**: Design documents from `/specs/001-podropsquare-is-a/`
**Prerequisites**: plan.md (✓), research.md (✓), data-model.md (✓), contracts/ (✓)

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → Loaded: Blazor WebAssembly + ASP.NET Core Web API + Azure Table Storage stack
2. Load optional design documents:
   → data-model.md: Extracted entities: GameSession, Block, ScoreEntry, Leaderboard
   → contracts/: Extracted endpoints: POST /scores, GET /scores/top10, GET /health
   → research.md: Extracted decisions: Matter.js physics, Azure Table Storage, Azurite
3. Generate tasks by category:
   → Setup: project init, dependencies, Azurite
   → Tests: contract tests, integration tests, physics tests
   → Core: models, services, components, physics interop
   → Integration: API endpoints, table storage, middleware
   → Polish: unit tests, performance, E2E validation
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness: ✓ All contracts have tests, ✓ All entities have models
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **Web app**: `backend/src/`, `frontend/src/` (per plan.md structure)
- Paths below follow Option 2 structure from implementation plan

## Phase 3.1: Setup & Infrastructure ✅ COMPLETED
- [x] T001 Create project structure per implementation plan with backend/ and frontend/ directories
- [x] T002 Initialize backend ASP.NET Core Web API project with .NET 8 in backend/src/Po.PoDropSquare.Api/
- [x] T003 Initialize frontend Blazor WebAssembly project with .NET 8 in frontend/src/Po.PoDropSquare.Blazor/
- [x] T004 Add Azure.Data.Tables NuGet package to backend/src/Po.PoDropSquare.Data/
- [x] T005 Add Matter.js CDN reference and create physics interop JS in frontend/src/Po.PoDropSquare.Blazor/wwwroot/js/
- [x] T006 Install and configure Azurite emulator for local development
- [x] T007 Configure Serilog structured logging in backend/src/Po.PoDropSquare.Api/
- [x] T008 Set up xUnit test projects for backend integration tests in backend/tests/
- [x] T009 Set up bUnit test project for Blazor components in frontend/tests/Po.PoDropSquare.Blazor.Tests/

## Phase 3.2: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.3
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**
- [ ] T010 [P] Contract test POST /api/scores in backend/tests/Po.PoDropSquare.Api.Tests/ScoreSubmissionContractTests.cs
- [ ] T011 [P] Contract test GET /api/scores/top10 in backend/tests/Po.PoDropSquare.Api.Tests/LeaderboardContractTests.cs
- [ ] T012 [P] Contract test GET /api/health in backend/tests/Po.PoDropSquare.Api.Tests/HealthCheckContractTests.cs
- [ ] T013 [P] Integration test complete gameplay session in backend/tests/Po.PoDropSquare.Api.Tests/GameplayIntegrationTests.cs
- [ ] T014 [P] Integration test cross-platform input handling in frontend/tests/Po.PoDropSquare.Blazor.Tests/InputHandlingTests.cs
- [ ] T015 [P] Integration test physics simulation consistency in frontend/tests/Po.PoDropSquare.Blazor.Tests/PhysicsIntegrationTests.cs
- [ ] T016 [P] Integration test timer accuracy and danger countdown in frontend/tests/Po.PoDropSquare.Blazor.Tests/TimerSystemTests.cs

## Phase 3.3: Core Data Models (ONLY after tests are failing)
- [ ] T017 [P] ScoreEntry entity with Azure Table properties in backend/src/Po.PoDropSquare.Core/Entities/ScoreEntry.cs
- [ ] T018 [P] Leaderboard entity with Azure Table properties in backend/src/Po.PoDropSquare.Core/Entities/LeaderboardEntry.cs
- [ ] T019 [P] GameSession model for client-side state in frontend/src/Po.PoDropSquare.Blazor/Models/GameSession.cs
- [ ] T020 [P] Block model for physics objects in frontend/src/Po.PoDropSquare.Blazor/Models/Block.cs
- [ ] T021 [P] API contract DTOs (ScoreSubmissionRequest, LeaderboardResponse) in backend/src/Po.PoDropSquare.Core/Contracts/

## Phase 3.4: Data Access & Services
- [ ] T022 [P] Azure Table Storage service for score operations in backend/src/Po.PoDropSquare.Data/ScoreTableService.cs
- [ ] T023 [P] Azure Table Storage service for leaderboard operations in backend/src/Po.PoDropSquare.Data/LeaderboardTableService.cs
- [ ] T024 [P] Score validation service with anti-cheat measures in backend/src/Po.PoDropSquare.Services/ScoreValidationService.cs
- [ ] T025 [P] Leaderboard management service in backend/src/Po.PoDropSquare.Services/LeaderboardService.cs
- [ ] T026 [P] Game engine service for client-side game state in frontend/src/Po.PoDropSquare.Blazor/Services/GameEngineService.cs
- [ ] T027 [P] Physics interop service for Matter.js bridge in frontend/src/Po.PoDropSquare.Blazor/Services/PhysicsInteropService.cs
- [ ] T028 [P] Audio service for sound effects and feedback in frontend/src/Po.PoDropSquare.Blazor/Services/AudioService.cs

## Phase 3.5: API Endpoints Implementation ✅ COMPLETED
- [x] T029 POST /api/scores endpoint with validation in backend/src/Po.PoDropSquare.Api/Controllers/ScoresController.cs
- [x] T030 GET /api/scores/top10 endpoint with caching in backend/src/Po.PoDropSquare.Api/Controllers/ScoresController.cs
- [x] T031 GET /api/health endpoint with dependency checks in backend/src/Po.PoDropSquare.Api/Controllers/HealthController.cs
- [x] T032 Global error handling middleware with Serilog in backend/src/Po.PoDropSquare.Api/Middleware/ErrorHandlingMiddleware.cs
- [x] T033 Rate limiting middleware for score submissions in backend/src/Po.PoDropSquare.Api/Middleware/RateLimitingMiddleware.cs

## Phase 3.6: Frontend Game Components ✅ COMPLETED
- [x] T034 [P] Main game canvas component with input handling in frontend/src/Po.PoDropSquare.Blazor/Components/GameCanvas.razor
- [x] T035 [P] Timer display component with countdown warnings in frontend/src/Po.PoDropSquare.Blazor/Components/TimerDisplay.razor
- [x] T036 [P] Leaderboard display component in frontend/src/Po.PoDropSquare.Blazor/Components/LeaderboardDisplay.razor
- [x] T037 [P] Score submission modal component in frontend/src/Po.PoDropSquare.Blazor/Components/ScoreSubmissionModal.razor
- [x] T038 [P] Game over screen component with restart functionality in frontend/src/Po.PoDropSquare.Blazor/Components/GameOverScreen.razor
- [x] T039 Main game page integrating all components in frontend/src/Po.PoDropSquare.Blazor/Pages/Game.razor

## Phase 3.7: Physics Integration ✅ COMPLETED
- [x] T040 Matter.js initialization and world setup in frontend/src/Po.PoDropSquare.Blazor/wwwroot/js/physics-engine.js
- [x] T041 Block creation and physics properties in frontend/src/Po.PoDropSquare.Blazor/wwwroot/js/physics-engine.js
- [x] T042 Collision detection and goal line monitoring in frontend/src/Po.PoDropSquare.Blazor/wwwroot/js/physics-engine.js
- [x] T043 60 FPS update loop with requestAnimationFrame in frontend/src/Po.PoDropSquare.Blazor/wwwroot/js/physics-engine.js
- [x] T044 C# to JavaScript interop bridge in frontend/src/Po.PoDropSquare.Blazor/Services/PhysicsInteropService.cs

## Phase 3.8: Integration & Configuration
- [ ] T045 Azure Table Storage connection configuration in backend/src/Po.PoDropSquare.Api/appsettings.json
- [ ] T046 Dependency injection setup for all services in backend/src/Po.PoDropSquare.Api/Program.cs
- [ ] T047 CORS configuration for Blazor frontend in backend/src/Po.PoDropSquare.Api/Program.cs
- [ ] T048 Health check dependencies registration in backend/src/Po.PoDropSquare.Api/Program.cs
- [ ] T049 Frontend API client service configuration in frontend/src/Po.PoDropSquare.Blazor/Program.cs

## Phase 3.9: Polish & Performance
- [ ] T050 [P] Unit tests for score validation logic in backend/tests/Po.PoDropSquare.Core.Tests/ScoreValidationTests.cs
- [ ] T051 [P] Unit tests for game engine state management in frontend/tests/Po.PoDropSquare.Blazor.Tests/GameEngineTests.cs
- [ ] T052 [P] Unit tests for physics interop service in frontend/tests/Po.PoDropSquare.Blazor.Tests/PhysicsInteropTests.cs
- [ ] T053 [P] Performance tests for API endpoints (<200ms target) in backend/tests/Po.PoDropSquare.E2E.Tests/PerformanceTests.cs
- [ ] T054 [P] End-to-end gameplay validation using Playwright in backend/tests/Po.PoDropSquare.E2E.Tests/GameplayE2ETests.cs
- [ ] T055 Execute quickstart.md validation checklist for all functional requirements
- [ ] T056 Code cleanup: remove unused imports, refactor files >500 lines, update documentation

## Dependencies
**Critical Path:**
- Setup (T001-T009) before everything
- Tests (T010-T016) before implementation (T017+)
- Models (T017-T021) before services (T022-T028)
- Services before API endpoints (T029-T033)
- Core components (T034-T038) before integration (T039)
- Physics setup (T040-T043) before interop bridge (T044)
- All core features before configuration (T045-T049)
- Implementation complete before polish (T050-T056)

**Blocking Relationships:**
- T017-T021 block T022-T028 (models before services)
- T022-T025 block T029-T031 (data services before API endpoints)
- T026-T028 block T034-T038 (client services before components)
- T040-T043 block T044 (JS physics before C# interop)
- T034-T038 block T039 (components before main page)
- T029-T031, T039 block T045-T049 (features before configuration)

## Parallel Execution Examples

### Phase 3.2 - All Tests in Parallel
```
# Launch T010-T016 together (different test files):
Task: "Contract test POST /api/scores in backend/tests/Po.PoDropSquare.Api.Tests/ScoreSubmissionContractTests.cs"
Task: "Contract test GET /api/scores/top10 in backend/tests/Po.PoDropSquare.Api.Tests/LeaderboardContractTests.cs"
Task: "Contract test GET /api/health in backend/tests/Po.PoDropSquare.Api.Tests/HealthCheckContractTests.cs"
Task: "Integration test complete gameplay session in backend/tests/Po.PoDropSquare.Api.Tests/GameplayIntegrationTests.cs"
Task: "Integration test cross-platform input handling in frontend/tests/Po.PoDropSquare.Blazor.Tests/InputHandlingTests.cs"
Task: "Integration test physics simulation consistency in frontend/tests/Po.PoDropSquare.Blazor.Tests/PhysicsIntegrationTests.cs"
Task: "Integration test timer accuracy and danger countdown in frontend/tests/Po.PoDropSquare.Blazor.Tests/TimerSystemTests.cs"
```

### Phase 3.3 - All Models in Parallel
```
# Launch T017-T021 together (different model files):
Task: "ScoreEntry entity with Azure Table properties in backend/src/Po.PoDropSquare.Core/Entities/ScoreEntry.cs"
Task: "Leaderboard entity with Azure Table properties in backend/src/Po.PoDropSquare.Core/Entities/LeaderboardEntry.cs"
Task: "GameSession model for client-side state in frontend/src/Po.PoDropSquare.Blazor/Models/GameSession.cs"
Task: "Block model for physics objects in frontend/src/Po.PoDropSquare.Blazor/Models/Block.cs"
Task: "API contract DTOs in backend/src/Po.PoDropSquare.Core/Contracts/"
```

### Phase 3.6 - All Components in Parallel
```
# Launch T034-T038 together (different component files):
Task: "Main game canvas component with input handling in frontend/src/Po.PoDropSquare.Blazor/Components/GameCanvas.razor"
Task: "Timer display component with countdown warnings in frontend/src/Po.PoDropSquare.Blazor/Components/TimerDisplay.razor"
Task: "Leaderboard display component in frontend/src/Po.PoDropSquare.Blazor/Components/LeaderboardDisplay.razor"
Task: "Score submission modal component in frontend/src/Po.PoDropSquare.Blazor/Components/ScoreSubmissionModal.razor"
Task: "Game over screen component with restart functionality in frontend/src/Po.PoDropSquare.Blazor/Components/GameOverScreen.razor"
```

## Task Generation Rules Applied

✅ **From Contracts**: Each API endpoint → contract test + implementation task  
✅ **From Data Model**: Each entity → model creation task [P]  
✅ **From User Stories**: Each acceptance scenario → integration test [P]  
✅ **Ordering**: Setup → Tests → Models → Services → Endpoints → Components → Integration → Polish  
✅ **Dependencies**: Tests before implementation, models before services, core before polish  

## Validation Checklist
*GATE: All items verified*

- [x] All contracts (POST /scores, GET /scores/top10, GET /health) have corresponding tests
- [x] All entities (ScoreEntry, LeaderboardEntry, GameSession, Block) have model tasks
- [x] All tests (T010-T016) come before implementation (T017+)
- [x] Parallel tasks [P] are truly independent (different files)
- [x] Each task specifies exact file path
- [x] No task modifies same file as another [P] task
- [x] TDD workflow enforced: failing tests required before implementation
- [x] All functional requirements covered through tasks and validation

**Total Tasks**: 56 tasks covering complete MVP implementation  
**Estimated Timeline**: 4-6 weeks for full implementation with testing  
**Critical Path**: Setup → Tests → Core Implementation → Integration → Validation