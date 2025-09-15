# Implementation Plan: PoDropSquare Physics-Based Tower Building Game

**Branch**: `001-podropsquare-is-a` | **Date**: September 13, 2025 | **Spec**: /specs/001-podropsquare-is-a/spec.md
**Input**: Feature specification from `/specs/001-podropsquare-is-a/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → Loaded: PoDropSquare physics-based tower building game specification
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detected Project Type: web application (frontend + backend)
   → Set Structure Decision: Option 2 (Web application structure)
3. Evaluate Constitution Check section below
   → Constitution template found but not configured for this project
   → Update Progress Tracking: Initial Constitution Check
4. Execute Phase 0 → research.md
   → No NEEDS CLARIFICATION remaining in spec
5. Execute Phase 1 → contracts, data-model.md, quickstart.md, .github/copilot-instructions.md
6. Re-evaluate Constitution Check section
   → Update Progress Tracking: Post-Design Constitution Check
7. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
8. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Primary requirement: Physics-based puzzle game where players drop colored blocks to build stable towers while racing against dual timers (20-second survival + 2-second danger countdown). Technical approach includes Blazor WebAssembly frontend with Matter.js physics engine via JSInterop, ASP.NET Core Web API backend with Azure Table Storage, and Azure App Service hosting.

## Technical Context
**Language/Version**: .NET 8 (C# 12), JavaScript ES2022 for physics integration  
**Primary Dependencies**: Blazor WebAssembly, ASP.NET Core Web API, Matter.js (physics), Azure.Data.Tables, Azurite emulator  
**Storage**: Azure Table Storage for leaderboard persistence (Azurite for local development), local browser storage for offline caching  
**Testing**: xUnit for backend, bUnit for Blazor components, Playwright for end-to-end testing  
**Target Platform**: Modern web browsers (Chrome 90+, Firefox 88+, Safari 14+, Edge 90+) with WebAssembly support  
**Project Type**: web - determines source structure (frontend + backend)  
**Performance Goals**: 60 FPS gameplay, <50ms input response, <3s initial load, <200ms API response  
**Constraints**: Sub-3-second load times, 99.9% uptime, cross-platform touch/mouse support, offline capability  
**Scale/Scope**: Casual gaming audience, top-10 leaderboard, 20-second game sessions, real-time physics simulation

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Simplicity**:
- Projects: 2 (frontend Blazor WebAssembly, backend Web API) - within max 3 limit
- Using framework directly? Yes - Blazor and ASP.NET Core used directly without wrapper abstractions
- Single data model? Yes - unified entities across frontend/backend with minimal DTOs for API boundaries
- Avoiding patterns? Yes - direct Entity Framework usage, no Repository pattern unless proven necessary

**Architecture**:
- EVERY feature as library? Game logic will be componentized into reusable Blazor components and service classes
- Libraries listed: GameEngine (physics/game state), ScoreService (leaderboard), AudioService (sound effects), PhysicsInterop (Matter.js bridge)
- CLI per library: Not applicable for web game - browser-based interaction only
- Library docs: Component documentation will follow standard .NET XML comments

**Testing (NON-NEGOTIABLE)**:
- RED-GREEN-Refactor cycle enforced? Yes - tests written first, must fail, then implement
- Git commits show tests before implementation? Yes - commit structure will demonstrate TDD workflow
- Order: Contract→Integration→E2E→Unit strictly followed? Yes - API contracts first, then integration tests, E2E gameplay tests, unit tests for components
- Real dependencies? Yes - actual Azurite emulator for integration tests, real browser for E2E tests
- Integration tests for: API endpoints, physics integration, score persistence, cross-browser compatibility

**Observability**:
- Structured logging included? Yes - Serilog for backend, browser console structured logging for frontend
- Frontend logs → backend? Yes - client errors will be reported to backend for unified monitoring
- Error context sufficient? Yes - detailed error context with game state for debugging

**Versioning**:
- Version number assigned? 1.0.0 (initial release)
- BUILD increments on every change? Yes - semantic versioning with automated build increments
- Breaking changes handled? Yes - API versioning strategy and backward compatibility tests

## Project Structure

### Documentation (this feature)
```
specs/001-podropsquare-is-a/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Option 2: Web application (frontend + backend detected)
backend/
├── src/
│   ├── Po.PoDropSquare.Api/          # Web API project
│   ├── Po.PoDropSquare.Core/         # Domain models and business logic
│   ├── Po.PoDropSquare.Data/         # Azure Table Storage data access
│   └── Po.PoDropSquare.Services/     # Application services
└── tests/
    ├── Po.PoDropSquare.Api.Tests/    # API integration tests
    ├── Po.PoDropSquare.Core.Tests/   # Unit tests for business logic
    └── Po.PoDropSquare.E2E.Tests/    # End-to-end tests

frontend/
├── src/
│   ├── Po.PoDropSquare.Blazor/       # Main Blazor WebAssembly project
│   ├── Components/                   # Reusable Blazor components
│   ├── Services/                     # Client-side services
│   ├── Models/                       # Shared data models
│   └── wwwroot/                      # Static assets, JS interop files
└── tests/
    ├── Po.PoDropSquare.Blazor.Tests/ # Blazor component unit tests
    └── Po.PoDropSquare.E2E.Tests/    # Browser-based E2E tests
```

**Structure Decision**: Option 2 (Web application) - frontend Blazor WebAssembly + backend ASP.NET Core Web API

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context**: No NEEDS CLARIFICATION items remain - all technical decisions specified in user input

2. **Research tasks completed**:
   - Matter.js integration patterns with Blazor WebAssembly via JSInterop
   - Best practices for real-time physics simulation in browser games
   - Azure Table Storage performance optimization for leaderboard queries
   - Blazor WebAssembly deployment and hosting strategies on Azure App Service
   - Cross-platform input handling patterns for touch and mouse events

3. **Key research findings consolidated**:
   - Decision: Matter.js via JSInterop for physics simulation
   - Rationale: Proven physics engine with excellent web performance, mature Blazor interop patterns
   - Alternatives considered: Box2D.js (more complex setup), custom physics (development overhead)

**Output**: research.md with all technical decisions documented and justified

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Game Session: Timer state, block positions, player interaction state
   - Block: Position, rotation, color, physics properties
   - Score Entry: Player initials, survival time, timestamp, validation data
   - Leaderboard: Ranked score collection with top-10 filtering

2. **Generate API contracts** from functional requirements:
   - POST /api/scores - Submit score with validation
   - GET /api/scores/top10 - Retrieve leaderboard
   - GET /api/health - System health check
   - OpenAPI specification with request/response schemas

3. **Generate contract tests** from contracts:
   - Score submission validation tests (must fail initially)
   - Leaderboard retrieval tests
   - Health check availability tests
   - Rate limiting and fraud prevention tests

4. **Extract test scenarios** from user stories:
   - Complete gameplay session integration test
   - Cross-platform input handling test
   - Physics simulation consistency test
   - Timer accuracy and danger countdown test

5. **Update agent file incrementally**:
   - Update .github/copilot-instructions.md with current technical stack
   - Add Blazor WebAssembly, Matter.js, PostgreSQL context
   - Preserve existing project guidelines and coding standards

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, updated .github/copilot-instructions.md

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Load `/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each API contract → contract test task [P]
- Each entity (GameSession, Block, ScoreEntry) → model creation task [P] 
- Each user story → integration test task
- Physics integration → specialized physics interop tasks
- Frontend components → Blazor component creation tasks
- Implementation tasks to make tests pass

**Ordering Strategy**:
- TDD order: Contract tests → Integration tests → Unit tests → Implementation
- Dependency order: Data models → Services → API endpoints → Frontend components → Physics integration
- Mark [P] for parallel execution (independent files/components)
- Physics setup early due to complexity and validation requirements

**Estimated Output**: 35-40 numbered, ordered tasks in tasks.md covering:
- Database setup and Entity Framework configuration
- API contract implementation and testing
- Blazor component development with physics integration
- Cross-platform input handling
- Audio/visual feedback systems
- Leaderboard and scoring functionality
- End-to-end gameplay testing and validation

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*No constitutional violations identified that require justification*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | N/A | N/A |

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*