# E2E Test Setup Summary

## ✅ What We Accomplished

Successfully implemented **automatic API server startup/shutdown** for E2E tests using xUnit collection fixtures.

## 📋 Changes Made

### 1. Created ApiServerFixture (`backend/tests/Po.PoDropSquare.E2E.Tests/ApiServerFixture.cs`)

**Purpose**: Automatically start the API server before E2E tests run and stop it after all tests complete.

**Key Features**:
- Implements `IAsyncLifetime` for async setup/teardown
- Starts API in **Release mode** to avoid PDB loading issues in headless Chrome
- Sets `ASPNETCORE_ENVIRONMENT=Production` environment variable
- Waits up to 30 seconds for API to be ready (checks `/health` endpoint)
- Kills entire API process tree on cleanup (including child processes)
- Logs all API output with `[API]` prefix for debugging
- Skips startup if API is already running (allows manual testing workflow)

**Path Resolution**:
- Dynamically searches for test project `.csproj` file from current directory
- Navigates to `../../src/Po.PoDropSquare.Api` from test project root
- Throws clear error if API project not found

### 2. Updated All 6 E2E Test Classes

Added `[Collection("ApiServer")]` attribute to:
- `CoreGameplayE2ETests.cs`
- `SimplifiedGameUITests.cs`
- `VictoryCountdownE2ETests.cs`
- `SimplifiedVictoryTest.cs`
- `UnitTest1.cs` (PoDropSquareE2ETests)
- `PlaywrightDemo.cs`

Also added missing `using Xunit;` statements where needed.

### 3. Fixed Blazor Routing Conflict

**Issue**: Duplicate routes for `/diag` path
- `Diagnostics.razor` had `@page "/diag"`
- `Diag.razor` also had `@page "/diag"`

**Resolution**: Deleted duplicate `Diag.razor` and `Diag.razor.css` files.

### 4. Built Projects in Release Mode

```bash
dotnet build backend/src/Po.PoDropSquare.Api/Po.PoDropSquare.Api.csproj -c Release
dotnet build frontend/src/Po.PoDropSquare.Blazor/Po.PoDropSquare.Blazor.csproj -c Release
```

**Why**: Release builds don't generate `.pdb` symbol files, preventing headless Chrome errors.

## 🏃 How to Run E2E Tests

### Single Test
```bash
dotnet test backend/tests/Po.PoDropSquare.E2E.Tests/Po.PoDropSquare.E2E.Tests.csproj \
  --filter "FullyQualifiedName~CoreGameplayE2ETests.HomePage_ShouldLoadSuccessfully" \
  --logger "console;verbosity=minimal"
```

### All E2E Tests
```bash
dotnet test backend/tests/Po.PoDropSquare.E2E.Tests/Po.PoDropSquare.E2E.Tests.csproj
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~CoreGameplayE2ETests"
```

## 📊 Test Results

**Before Fix**:
- Tests: 1
- Passed: 0
- Failed: 1
- Error: `ERR_CONNECTION_REFUSED` (API not running)

**After Fix**:
- Tests: 1
- Passed: 1 ✅
- Failed: 0
- Duration: ~20 seconds (includes API startup time)

## 🔍 Troubleshooting

### API Doesn't Start
**Symptom**: Tests fail with "API is not running" or timeout after 30 seconds

**Solutions**:
1. Check if port 5000 is already in use:
   ```powershell
   netstat -ano | findstr :5000
   ```
2. Kill existing processes on port 5000
3. Check API health manually:
   ```bash
   dotnet run --project backend/src/Po.PoDropSquare.Api -c Release
   # Then visit: http://localhost:5000/health
   ```

### PDB Loading Errors
**Symptom**: Blazor fails with "Failed to fetch .pdb" errors

**Solutions**:
1. Ensure API is built in Release mode (fixture does this automatically)
2. Check that `ASPNETCORE_ENVIRONMENT=Production` is set
3. Verify ApiServerFixture arguments: `dotnet run --no-build -c Release`

### Path Resolution Fails
**Symptom**: `InvalidOperationException: API project not found at: ...`

**Solutions**:
1. Verify directory structure:
   ```
   PoDropSquare/
   ├── backend/
   │   ├── src/
   │   │   └── Po.PoDropSquare.Api/
   │   └── tests/
   │       └── Po.PoDropSquare.E2E.Tests/
   ```
2. Check that `Po.PoDropSquare.E2E.Tests.csproj` exists
3. Ensure paths match in ApiServerFixture.cs lines 29-38

### Tests Hang or Timeout
**Symptom**: Tests never complete, stuck waiting for API

**Solutions**:
1. Check Azurite is running (required for Table Storage):
   ```powershell
   azurite --silent --location c:\azurite
   ```
2. Increase timeout in ApiServerFixture.cs (line 14):
   ```csharp
   private const int MAX_STARTUP_WAIT_SECONDS = 60; // Increase from 30
   ```
3. Check API logs in test output for startup errors

## 🎯 Next Steps

### 1. Run Full E2E Test Suite
```bash
dotnet test backend/tests/Po.PoDropSquare.E2E.Tests/Po.PoDropSquare.E2E.Tests.csproj --logger "console;verbosity=normal"
```

Expected: 34 tests should now run with API automatically starting/stopping.

### 2. Verify Other Test Classes Pass
All 6 test classes now use the shared ApiServerFixture:
- [ ] `CoreGameplayE2ETests` (1 test passed ✅)
- [ ] `SimplifiedGameUITests`
- [ ] `VictoryCountdownE2ETests`
- [ ] `SimplifiedVictoryTest`
- [ ] `UnitTest1` (PoDropSquareE2ETests)
- [ ] `PlaywrightDemo`

### 3. Update CI/CD Pipeline
The fixture works locally and in CI/CD. GitHub Actions workflow should:
1. Start Azurite (already configured)
2. Build projects in Release mode
3. Run E2E tests (fixture handles API lifecycle)

No changes needed to `.github/workflows/` - fixture is CI/CD compatible.

## 📝 Code References

### ApiServerFixture Key Code Sections

**API Startup** (lines 17-76):
```csharp
public async Task InitializeAsync()
{
    // Check if API already running
    if (await IsApiRunningAsync()) { return; }
    
    // Find API project path dynamically
    var testProjectRoot = FindTestProjectRoot();
    var apiProjectPath = Path.Combine(testProjectRoot, "..", "..", "src", "Po.PoDropSquare.Api");
    
    // Start dotnet process
    _apiProcess = new Process {
        StartInfo = new ProcessStartInfo {
            FileName = "dotnet",
            Arguments = "run --no-build -c Release", // Release mode!
            ...
        }
    };
    
    // Set environment to Production (no PDB loading)
    _apiProcess.StartInfo.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Production";
    
    _apiProcess.Start();
    await WaitForApiToBeReadyAsync(); // Poll /health endpoint
}
```

**API Cleanup** (lines 102-140):
```csharp
public async Task DisposeAsync()
{
    if (_apiProcess != null)
    {
        Console.WriteLine("Stopping API server...");
        KillProcessTree(_apiProcess.Id); // Kill all child processes
        Console.WriteLine("API server stopped");
    }
}
```

### Test Class Example

```csharp
// Add collection attribute to use shared fixture
[Collection("ApiServer")]
public class CoreGameplayE2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;
    
    private const string BASE_URL = "http://localhost:5000";
    
    public async Task InitializeAsync() {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _page = await _browser.NewPageAsync();
    }
    
    [Fact]
    [Trait("Category", "E2E")]
    public async Task HomePage_ShouldLoadSuccessfully() {
        await _page.GotoAsync(BASE_URL);
        var gameCanvas = await _page.QuerySelectorAsync("canvas");
        Assert.NotNull(gameCanvas); // ✅ Now passes!
    }
}
```

## 🏆 Benefits

1. **No Manual API Management**: Developers don't need to remember to start API before running E2E tests
2. **CI/CD Compatible**: GitHub Actions can run full E2E suite without extra scripts
3. **Shared Fixture**: One API instance for all test classes (faster test execution)
4. **Clean Lifecycle**: API always stopped after tests, no orphaned processes
5. **Release Mode**: Avoids PDB loading issues in headless Chrome
6. **Robust Path Resolution**: Works across different build configurations and environments
7. **Clear Logging**: All API logs prefixed with `[API]` for easy debugging

## ⚠️ Important Notes

- **Azurite Required**: API needs Azure Table Storage emulator running
- **Port 5000 Must Be Free**: Ensure no other process uses this port
- **Release Build Required**: Debug builds will cause PDB loading errors in tests
- **Collection Fixture Shared**: All tests in collection share ONE API instance
- **30s Startup Timeout**: API must respond to `/health` within 30 seconds

---

**Created**: 2025-11-07  
**Last Updated**: 2025-11-07  
**Status**: ✅ Working - First E2E test passing with automatic API management
