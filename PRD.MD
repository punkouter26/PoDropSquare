# Product Requirements Document (PRD)
## PoDropSquare

**Version**: 1.0  
**Last Updated**: October 27, 2025  
**Owner**: punkouter26  
**Status**: ✅ Development Complete - Ready for Deployment

---

## 📋 Executive Summary

**PoDropSquare** is a modern, high-performance drop square physics game built with Blazor WebAssembly and ASP.NET Core. Players stack falling squares to achieve the highest score while competing on a global leaderboard. The application showcases enterprise-grade architecture with Azure cloud integration, comprehensive monitoring, and automated CI/CD deployment.

### Key Differentiators
- ✨ **Browser-based** - No installation required
- 🎮 **Physics Engine** - Realistic Matter.js physics simulation
- 🏆 **Global Leaderboard** - Azure Table Storage-backed rankings
- 📊 **Production Monitoring** - Application Insights telemetry
- 🚀 **Auto-Deployment** - GitHub Actions CI/CD pipeline
- 📱 **Responsive Design** - Works on desktop and mobile

---

## 🎯 Product Vision

### Mission Statement
Create an engaging, accessible browser game that demonstrates modern .NET 9.0 capabilities while providing a fun, competitive gaming experience.

### Target Audience
- **Primary**: Developers learning Blazor WebAssembly and .NET architecture
- **Secondary**: Casual gamers looking for quick, competitive gameplay
- **Tertiary**: DevOps engineers studying Azure deployment patterns

### Success Metrics
| Metric | Target | Measurement |
|--------|--------|-------------|
| Page Load Time | <2 seconds | Application Insights |
| API Response Time (P95) | <200ms | Application Insights |
| Daily Active Users | 100+ | KQL Query #19 |
| Error Rate | <1% | KQL Query #9 |
| Uptime | >99.5% | KQL Query #16 |
| User Session Duration | >5 minutes | KQL Query #3 |

---

## 🎮 Game Mechanics

### Core Gameplay

**Objective**: Stack falling squares to build the tallest tower without letting them fall off-screen.

**Rules**:
1. Squares drop from the top of the screen at regular intervals
2. Click/tap to release the square at current position
3. Squares obey realistic physics (gravity, friction, collisions)
4. Points awarded based on tower height and stability
5. Game ends when squares fall off-screen boundaries
6. Submit final score to global leaderboard with 3-letter initials

### Physics Parameters
- **Gravity**: Standard Matter.js gravity
- **Friction**: 0.3 (realistic stacking behavior)
- **Restitution**: 0.2 (minimal bounce)
- **Square Size**: 50x50 pixels
- **Drop Interval**: Configurable (default: 2 seconds)

### Scoring Algorithm
```
Base Score = Tower Height (pixels)
Bonus = Stability Factor (0-100)
Final Score = Base Score + Bonus
```

### Difficulty Progression
- Squares drop faster as score increases
- Smaller squares appear at higher levels
- Wind forces introduced at advanced stages (future feature)

---

## 🏗️ Technical Architecture

### Technology Stack

#### Frontend
- **Framework**: Blazor WebAssembly (.NET 9.0)
- **UI Library**: Radzen.Blazor components
- **Physics**: Matter.js (JavaScript interop)
- **Styling**: CSS3 with responsive design
- **State Management**: Component-based state

#### Backend
- **Framework**: ASP.NET Core 9.0 Web API
- **Logging**: Serilog with structured logging
- **Data Storage**: Azure Table Storage
- **Health Checks**: ASP.NET Core Health Checks
- **API Documentation**: Swagger/OpenAPI

#### Infrastructure
- **Cloud Provider**: Microsoft Azure
- **IaC**: Bicep templates
- **Deployment**: Azure Developer CLI (azd)
- **Hosting**: Azure App Service (F1 Free tier)
- **Monitoring**: Application Insights + Log Analytics
- **CI/CD**: GitHub Actions with OIDC authentication

### Architecture Diagram (Conceptual)

```
┌─────────────────────────────────────────────────────────────┐
│                    Browser (Client)                         │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Blazor WebAssembly Application                        │  │
│  │ ├─ Game UI (Canvas + Matter.js)                       │  │
│  │ ├─ Leaderboard Component                              │  │
│  │ ├─ Diagnostics Page                                   │  │
│  │ └─ Client-side Error Logging                          │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓ HTTPS
┌─────────────────────────────────────────────────────────────┐
│              Azure App Service (ASP.NET Core)               │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ API Controllers                                        │  │
│  │ ├─ /api/scores (POST, GET)                            │  │
│  │ ├─ /api/scores/top10 (GET)                            │  │
│  │ ├─ /api/scores/leaderboard (GET)                      │  │
│  │ ├─ /api/scores/player/{initials}/rank (GET)           │  │
│  │ ├─ /api/log/client (POST)                             │  │
│  │ ├─ /api/log/error (POST)                              │  │
│  │ └─ /api/health (GET)                                  │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Middleware Pipeline                                    │  │
│  │ ├─ Error Handling (RFC 7807)                          │  │
│  │ ├─ Rate Limiting                                      │  │
│  │ ├─ Request Logging (Serilog)                          │  │
│  │ └─ Response Caching                                   │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Services Layer                                         │  │
│  │ ├─ ScoreService                                       │  │
│  │ └─ LeaderboardService                                 │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Data Layer                                             │  │
│  │ ├─ ScoreRepository                                    │  │
│  │ └─ LeaderboardRepository                              │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                   Azure Resources                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Table      │  │     Log      │  │ Application  │      │
│  │   Storage    │  │  Analytics   │  │   Insights   │      │
│  │ (Leaderboard)│  │  Workspace   │  │ (Telemetry)  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

### Data Models

#### ScoreEntry (Azure Table Entity)
```csharp
{
  PartitionKey: "SCORES",
  RowKey: "{Guid}",
  PlayerInitials: "ABC",      // 3 chars, uppercase, alphanumeric
  Score: 1000,                 // Integer
  Timestamp: DateTime,         // Auto-generated
  GameDuration: 120,           // Seconds
  SquaresPlaced: 15            // Count
}
```

#### LeaderboardEntry
```csharp
{
  Rank: 1,
  PlayerInitials: "ABC",
  Score: 1000,
  Timestamp: DateTime
}
```

---

## 🔌 API Specification

### Base URL
- **Development**: `http://localhost:5000`
- **Production**: `https://app-podropsquare-prod-eastus.azurewebsites.net`

### Endpoints

#### 1. Submit Score
```http
POST /api/scores
Content-Type: application/json

{
  "playerInitials": "ABC",
  "score": 1000
}

Response 201 Created:
{
  "id": "guid",
  "playerInitials": "ABC",
  "score": 1000,
  "timestamp": "2025-10-27T12:00:00Z"
}
```

**Validation**:
- `playerInitials`: Required, 3 characters, alphanumeric, uppercase
- `score`: Required, positive integer, max 999999

#### 2. Get Top 10 Scores
```http
GET /api/scores/top10

Response 200 OK:
[
  {
    "playerInitials": "ABC",
    "score": 1000,
    "timestamp": "2025-10-27T12:00:00Z"
  },
  ...
]
```

#### 3. Get Full Leaderboard
```http
GET /api/scores/leaderboard?limit=100

Response 200 OK:
[
  {
    "rank": 1,
    "playerInitials": "ABC",
    "score": 1000,
    "timestamp": "2025-10-27T12:00:00Z"
  },
  ...
]
```

#### 4. Get Player Rank
```http
GET /api/scores/player/ABC/rank

Response 200 OK:
{
  "playerInitials": "ABC",
  "rank": 5,
  "score": 850,
  "totalPlayers": 100
}

Response 404 Not Found:
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Player with initials 'ABC' not found"
}
```

#### 5. Client Logging
```http
POST /api/log/client
Content-Type: application/json

{
  "message": "User clicked start button",
  "level": "Information",
  "data": {
    "buttonId": "start-btn"
  }
}

Response 200 OK
```

#### 6. Error Logging
```http
POST /api/log/error
Content-Type: application/json

{
  "errorMessage": "TypeError: Cannot read property 'x' of null",
  "stackTrace": "at Game.update (game.js:42)",
  "url": "/game",
  "userAgent": "Mozilla/5.0..."
}

Response 200 OK
```

#### 7. Health Check
```http
GET /api/health

Response 200 OK:
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "self": {
      "status": "Healthy",
      "description": "API is running"
    },
    "azure-table-storage": {
      "status": "Healthy",
      "description": "Successfully connected"
    },
    "memory": {
      "status": "Healthy",
      "description": "Working set: 120 MB"
    }
  }
}
```

---

## 🎨 User Interface Requirements

### Pages

#### 1. Home/Game Page (`/`)
- **Components**:
  - Game canvas (Matter.js physics)
  - Score display (current game)
  - Timer display
  - Start/Stop controls
  - Instructions panel
  
- **Interactions**:
  - Click/tap to drop square
  - Auto-start on page load
  - Real-time score updates
  - Game over modal with score submission

#### 2. High Scores Page (`/highscores`)
- **Components**:
  - Top 10 leaderboard table
  - Player rank lookup
  - Full leaderboard (paginated)
  - Last updated timestamp
  
- **Interactions**:
  - Search for player initials
  - Refresh leaderboard
  - Navigate to game page

#### 3. Diagnostics Page (`/diag`)
- **Components**:
  - Backend health status badge
  - System information
  - API response times
  - Connection status
  
- **Interactions**:
  - Refresh health check
  - View detailed metrics
  - Link to Application Insights (admin only)

### Responsive Design
- **Desktop** (>1024px): Full layout with sidebar
- **Tablet** (768-1024px): Stacked layout, touch-optimized
- **Mobile** (<768px): Single column, portrait orientation

### Accessibility
- WCAG 2.1 AA compliance
- Keyboard navigation support
- Screen reader friendly
- High contrast mode

---

## 🔒 Security Requirements

### Authentication & Authorization
- **Current**: No authentication (public leaderboard)
- **Future**: Optional user accounts with OAuth2

### Data Validation
- ✅ Input sanitization on all API endpoints
- ✅ Player initials: 3 chars, alphanumeric, uppercase only
- ✅ Score validation: Positive integer, max 999999
- ✅ Rate limiting: 100 requests/minute per IP

### Security Headers
- ✅ HTTPS enforcement
- ✅ Content Security Policy (CSP)
- ✅ X-Frame-Options: DENY
- ✅ X-Content-Type-Options: nosniff

### Data Privacy
- No personally identifiable information (PII) collected
- IP addresses logged for rate limiting (30-day retention)
- Optional: GDPR compliance for EU users

---

## 📊 Monitoring & Observability

### Key Performance Indicators (KPIs)

| KPI | Target | Alert Threshold |
|-----|--------|-----------------|
| API Latency (P95) | <200ms | >500ms |
| Error Rate | <1% | >5% |
| Availability | >99.5% | <99% |
| Page Load Time | <2s | >5s |
| Memory Usage | <500MB | >800MB |

### Telemetry Collection
- **Application Insights**: Requests, dependencies, exceptions
- **Serilog**: Structured logging to files and App Insights
- **Client Logs**: JavaScript errors, user actions
- **Custom Events**: Score submissions, game completions

### Dashboards
1. **Real-Time Monitoring** - Active users, error rate, latency
2. **Weekly Executive Summary** - DAU, retention, performance
3. **Game Analytics** - Scores, leaderboard activity, player ranks

### Alerts
1. **High Error Rate** (>5% in 5 minutes)
2. **Slow Performance** (P95 >1000ms)
3. **Dependency Failure** (Table Storage unavailable)
4. **No Health Checks** (0 requests in 10 minutes)

See `/docs/KQL-QUERIES.md` for 31 production-ready monitoring queries.

---

## 🚀 Deployment & Operations

### Environments

| Environment | Purpose | URL | Auto-Deploy |
|-------------|---------|-----|-------------|
| **Local** | Development | http://localhost:5000 | Manual |
| **Dev** | Feature testing | https://app-podropsquare-dev-eastus.azurewebsites.net | On push to `001-podropsquare-is-a` |
| **Prod** | Production | https://app-podropsquare-prod-eastus.azurewebsites.net | On push to `main` |

### CI/CD Pipeline

**GitHub Actions Workflow** (`.github/workflows/azure-dev.yml`):
1. **Build** - Compile, test, create artifacts
2. **Deploy** - Provision Azure resources, deploy app
3. **E2E Tests** - Run Playwright tests against live app

**Deployment Steps**:
```bash
# 1. Build and test
dotnet restore
dotnet build --configuration Release
dotnet test

# 2. Provision infrastructure
azd provision --no-prompt

# 3. Deploy application
azd deploy --no-prompt

# 4. Verify deployment
curl https://YOUR_APP/api/health
```

### Rollback Strategy
- **Option 1**: Redeploy previous commit via GitHub
- **Option 2**: Azure Portal deployment slots
- **Option 3**: `azd down` + redeploy from git tag

### Disaster Recovery
- **RTO**: 1 hour (Recovery Time Objective)
- **RPO**: 24 hours (Recovery Point Objective)
- **Backup**: Azure Table Storage geo-redundancy
- **Runbook**: `/docs/DISASTER-RECOVERY.md` (future)

---

## 📈 Roadmap & Future Features

### Phase 1: MVP ✅ (Complete)
- [x] Core game mechanics
- [x] Global leaderboard
- [x] Azure deployment
- [x] CI/CD pipeline
- [x] Monitoring & telemetry

### Phase 2: Enhanced Gameplay (Q1 2026)
- [ ] Difficulty levels (Easy, Medium, Hard)
- [ ] Power-ups (slow motion, freeze, ghost square)
- [ ] Achievement system
- [ ] Sound effects and music
- [ ] Animated backgrounds

### Phase 3: Social Features (Q2 2026)
- [ ] User accounts with OAuth2
- [ ] Friend leaderboards
- [ ] Challenge mode (1v1)
- [ ] Share score on social media
- [ ] Weekly tournaments

### Phase 4: Mobile App (Q3 2026)
- [ ] Native iOS app (MAUI)
- [ ] Native Android app (MAUI)
- [ ] Offline mode with sync
- [ ] Push notifications
- [ ] In-app purchases (cosmetics)

### Phase 5: Multiplayer (Q4 2026)
- [ ] Real-time multiplayer (SignalR)
- [ ] Spectator mode
- [ ] Replay system
- [ ] Leaderboard seasons
- [ ] Global tournaments

---

## 💰 Cost Analysis

### Azure Resources (Production)

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| App Service Plan | F1 (Free) | $0 |
| Storage Account | Standard LRS | $0.02/GB |
| Log Analytics | Pay-as-you-go | $2.30/GB |
| Application Insights | Pay-as-you-go | First 5GB free |
| **TOTAL** | | **~$10-20/month** |

### Scaling Costs

| Traffic Level | Users/Day | Est. Cost/Month |
|---------------|-----------|-----------------|
| MVP | <100 | $10 |
| Growth | 100-1000 | $20-50 |
| Popular | 1000-10000 | $50-200 |
| Viral | >10000 | $200-1000 |

### Cost Optimization
- ✅ Use F1 free tier for App Service
- ✅ Enable adaptive sampling in App Insights
- ✅ 30-day log retention
- ✅ Shared storage account across environments
- ✅ Auto-scale only on production

---

## 📚 Documentation

### Developer Documentation
- **README.md** - Project overview, getting started
- **AGENTS.MD** - AI coding agent best practices
- **/docs/KQL-QUERIES.md** - 31 monitoring queries
- **/docs/APPLICATION-INSIGHTS-SETUP.md** - Telemetry configuration
- **/docs/ARCHITECTURE.md** - Detailed architecture diagrams
- **Program.cs** - Quick KQL reference in code comments

### Deployment Documentation
- **.github/CICD-SETUP.md** - GitHub Actions configuration
- **.github/PHASE5-SUMMARY.md** - CI/CD implementation details
- **azure.yaml** - azd configuration
- **/infra/** - Bicep templates with inline comments

### API Documentation
- **Swagger UI** - Available at `/swagger` in development
- **OpenAPI Spec** - Auto-generated from controllers
- **PoDropSquare.http** - REST Client test file

---

## ✅ Definition of Done

### Feature Complete Criteria
- [ ] All acceptance criteria met
- [ ] Unit tests written and passing (>80% coverage)
- [ ] Integration tests passing
- [ ] E2E tests passing
- [ ] Code reviewed and approved
- [ ] Documentation updated
- [ ] Deployed to dev environment
- [ ] QA testing complete
- [ ] Performance validated (<200ms P95)
- [ ] Security scan passed
- [ ] Deployed to production

### Quality Gates
- **Build**: Must compile with zero warnings
- **Tests**: All tests must pass (unit, integration, E2E)
- **Coverage**: >80% code coverage
- **Performance**: P95 latency <200ms
- **Security**: No critical vulnerabilities
- **Accessibility**: WCAG 2.1 AA compliant

---

## 🤝 Stakeholders

| Role | Name | Responsibility |
|------|------|----------------|
| **Product Owner** | punkouter26 | Vision, priorities, acceptance |
| **Tech Lead** | punkouter26 | Architecture, code quality |
| **DevOps** | punkouter26 | CI/CD, infrastructure, monitoring |
| **QA** | GitHub Actions | Automated testing |
| **Designer** | TBD | UI/UX design (future) |

---

## 📝 Appendix

### Glossary
- **DAU**: Daily Active Users
- **P95**: 95th percentile (latency metric)
- **KQL**: Kusto Query Language (Azure logs)
- **azd**: Azure Developer CLI
- **OIDC**: OpenID Connect (authentication)
- **IaC**: Infrastructure as Code

### References
- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [Matter.js Physics Engine](https://brm.io/matter-js/)
- [Azure Table Storage](https://learn.microsoft.com/en-us/azure/storage/tables/)
- [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [GitHub Actions](https://docs.github.com/en/actions)

### Change Log

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-27 | Initial PRD - MVP complete |

---

**Status**: ✅ **APPROVED FOR PRODUCTION**  
**Next Review**: 2026-01-27 (Quarterly)  
**Document Owner**: punkouter26
