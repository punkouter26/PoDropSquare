# Test Organization Guide

> **xUnit Traits for test categorization and selective execution**

## 📋 Test Categories

All tests are now organized with `[Trait("Category", "...")]` attributes:

| Category | Purpose | Project(s) | Example Run Command |
|----------|---------|------------|---------------------|
| **Unit** | Isolated unit tests with mocked dependencies | `Core.Tests` | `dotnet test --filter "Category=Unit"` |
| **Integration** | API integration tests with WebApplicationFactory | `Api.Tests` | `dotnet test --filter "Category=Integration"` |
| **Component** | Blazor component tests with bUnit | `Blazor.Tests` | `dotnet test --filter "Category=Component"` |
| **E2E** | End-to-end browser tests with Playwright | `E2E.Tests` | `dotnet test --filter "Category=E2E"` |

## 🏷️ Additional Traits

Tests also have **Feature** traits for grouping by business capability:

| Feature Trait | Tests | Example |
|---------------|-------|---------|
| `Feature=ScoreSubmission` | Score submission tests | `dotnet test --filter "Feature=ScoreSubmission"` |
| `Feature=Leaderboard` | Leaderboard tests | `dotnet test --filter "Feature=Leaderboard"` |
| `Feature=PlayerRank` | Player ranking tests | `dotnet test --filter "Feature=PlayerRank"` |
| `Feature=GameplayIntegration` | Full gameplay tests | `dotnet test --filter "Feature=GameplayIntegration"` |
| `Feature=HealthCheck` | Health check tests | `dotnet test --filter "Feature=HealthCheck"` |

## 📊 Test Statistics (Phase 3.1 Complete)

```
Total Tests:     113
Files Modified:  16
Projects:        4 (Core, Api, Blazor, E2E)

Breakdown by Category:
  Unit:        1 test
  Integration: 56 tests
  Component:   23 tests
  E2E:         34 tests
```

## 🚀 Common Test Commands

### Run All Tests (Excluding E2E)
```powershell
# Fast - excludes slow E2E tests
dotnet test --filter "Category!=E2E"
```

### Run by Category
```powershell
# Unit tests only (fastest)
dotnet test --filter "Category=Unit"

# Integration tests (requires Azurite)
dotnet test --filter "Category=Integration"

# Component tests (Blazor)
dotnet test --filter "Category=Component"

# E2E tests (requires running app + Playwright)
dotnet test --filter "Category=E2E"
```

### Run by Feature
```powershell
# All score submission tests
dotnet test --filter "Feature=ScoreSubmission"

# All leaderboard tests  
dotnet test --filter "Feature=Leaderboard"
```

### Combine Filters
```powershell
# Integration tests for a specific feature
dotnet test --filter "Category=Integration&Feature=ScoreSubmission"

# All tests except E2E
dotnet test --filter "Category!=E2E"

# Unit OR Component tests
dotnet test --filter "Category=Unit|Category=Component"
```

## 🧪 Test Naming Convention

All tests follow the pattern: **`MethodName_StateUnderTest_ExpectedBehavior`**

### Examples

✅ **Good test names:**
```csharp
[Fact]
[Trait("Category", "Integration")]
[Trait("Feature", "ScoreSubmission")]
public async Task POST_Scores_WithValidRequest_ShouldReturn200AndScoreAccepted()

[Theory]
[Trait("Category", "Unit")]
[InlineData(-0.5)]
[InlineData(0)]
[InlineData(25)]
public void CalculateScore_WithInvalidSurvivalTime_ShouldThrowArgumentException(double invalidTime)
```

❌ **Bad test names:**
```csharp
public void Test1()  // Not descriptive
public void TestScores()  // Doesn't describe expected behavior
public void TestThatScoresWorkCorrectly()  // Too vague
```

## 📂 Test Project Structure

```
tests/
├── Po.PoDropSquare.Core.Tests/          # Unit tests
│   ├── UnitTest1.cs                     # [Trait("Category", "Unit")]
│   └── ...
│
├── Po.PoDropSquare.Api.Tests/           # Integration tests
│   ├── ScoreSubmissionContractTests.cs  # [Trait("Category", "Integration")]
│   ├── LeaderboardContractTests.cs      # [Trait("Feature", "Leaderboard")]
│   ├── HealthCheckContractTests.cs
│   └── ...
│
├── Po.PoDropSquare.Blazor.Tests/        # Component tests
│   ├── InputHandlingTests.cs           # [Trait("Category", "Component")]
│   ├── TimerSystemTests.cs
│   └── ...
│
└── Po.PoDropSquare.E2E.Tests/           # E2E tests
    ├── CoreGameplayE2ETests.cs          # [Trait("Category", "E2E")]
    ├── SimplifiedGameUITests.cs
    └── ...
```

## 🔄 Adding Traits to New Tests

### Manual Method
```csharp
[Fact]
[Trait("Category", "Integration")]  // Required: test category
[Trait("Feature", "MyFeature")]      // Optional: business feature
public async Task MyTest_WithCondition_ShouldDoSomething()
{
    // Test implementation
}
```

### Automated Method
```powershell
# Run the trait adder script after creating new tests
.\scripts\add-test-traits.ps1

# Preview changes first
.\scripts\add-test-traits.ps1 -DryRun
```

## 📈 CI/CD Integration

### GitHub Actions (Phase 4)
```yaml
- name: Run Unit Tests
  run: dotnet test --filter "Category=Unit" --logger trx

- name: Run Integration Tests
  run: dotnet test --filter "Category=Integration" --logger trx

- name: Run Component Tests
  run: dotnet test --filter "Category=Component" --logger trx

# E2E tests run separately after deployment
- name: Run E2E Tests
  run: dotnet test --filter "Category=E2E" --logger trx
```

### Local Pre-Commit Hook
```powershell
# Run fast tests before commit
dotnet test --filter "Category!=E2E" --no-build

# Or just unit tests (fastest)
dotnet test --filter "Category=Unit" --no-build
```

## 🎯 Test Coverage Goals

| Category | Target Coverage | Current | Status |
|----------|----------------|---------|--------|
| **Unit** | 90%+ | TBD | 🔜 Phase 3.3 |
| **Integration** | 80%+ | TBD | 🔜 Phase 3.3 |
| **Component** | 70%+ | TBD | 🔜 Phase 3.3 |
| **E2E** | Critical paths only | TBD | 🔜 Phase 3.3 |

## 🛠️ Troubleshooting

### Issue: "No tests matched the filter"
**Cause**: Tests don't have Trait attributes

**Solution**:
```powershell
# Add traits automatically
.\scripts\add-test-traits.ps1

# Rebuild
dotnet build

# Try again
dotnet test --filter "Category=Unit"
```

### Issue: E2E tests fail with Playwright errors
**Cause**: Playwright browsers not installed or app not running

**Solution**:
```powershell
# Install Playwright browsers (one-time)
dotnet build backend/tests/Po.PoDropSquare.E2E.Tests
pwsh backend/tests/Po.PoDropSquare.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install

# Start the app before running E2E tests
# Terminal 1:
dotnet run --project backend/src/Po.PoDropSquare.Api

# Terminal 2:
dotnet test --filter "Category=E2E"
```

### Issue: Integration tests fail with storage errors
**Cause**: Azurite not running

**Solution**:
```powershell
# Start Azurite
azurite --silent --location .

# Or use the script
.\scripts\start-local-dev.ps1 -SkipBuild
```

## 📚 References

- **xUnit Traits**: https://xunit.net/docs/running-tests-in-vs.html#filter-tests
- **Test Naming**: https://docs.microsoft.com/dotnet/core/testing/unit-testing-best-practices
- **bUnit**: https://bunit.dev/
- **Playwright**: https://playwright.dev/dotnet/

---

**Last Updated**: November 7, 2025  
**Phase 3.1**: ✅ Complete - 113 tests organized with Traits
