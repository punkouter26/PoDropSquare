# Generate Code Coverage Report
# This script runs tests with coverage collection and generates an HTML report

param(
    [Parameter(Mandatory=$false)]
    [ValidateRange(0, 100)]
    [int]$MinimumCoverage = 80,
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeE2E,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild,
    
    [Parameter(Mandatory=$false)]
    [switch]$NoOpen
)

Write-Host "📊 PoDropSquare - Code Coverage Report Generator" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to root directory
$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

# Configuration
$coverageDir = Join-Path $rootDir "docs" "coverage"
$coverageResults = Join-Path $rootDir "coverage-results"
$reportHtml = Join-Path $coverageDir "index.html"

# Clean previous coverage data
Write-Host "🧹 Cleaning previous coverage data..." -ForegroundColor Yellow
if (Test-Path $coverageResults) {
    Remove-Item -Recurse -Force $coverageResults
}
if (Test-Path $coverageDir) {
    Remove-Item -Recurse -Force $coverageDir
}
New-Item -ItemType Directory -Force -Path $coverageResults | Out-Null
New-Item -ItemType Directory -Force -Path $coverageDir | Out-Null

# Build solution if needed
if (-not $SkipBuild) {
    Write-Host "🔨 Building solution..." -ForegroundColor Cyan
    dotnet build --configuration Release
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
}

# Test projects to include in coverage
$testProjects = @(
    "backend\tests\Po.PoDropSquare.Api.Tests\Po.PoDropSquare.Api.Tests.csproj",
    "backend\tests\Po.PoDropSquare.Core.Tests\Po.PoDropSquare.Core.Tests.csproj",
    "frontend\tests\Po.PoDropSquare.Blazor.Tests\Po.PoDropSquare.Blazor.Tests.csproj"
)

# Conditionally include E2E tests
if ($IncludeE2E) {
    $testProjects += "backend\tests\Po.PoDropSquare.E2E.Tests\Po.PoDropSquare.E2E.Tests.csproj"
    Write-Host "⚠️  Including E2E tests (this may take longer)..." -ForegroundColor Yellow
} else {
    Write-Host "⏭️  Skipping E2E tests (use -IncludeE2E to include)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🧪 Running tests with coverage collection..." -ForegroundColor Cyan
Write-Host "Projects to test: $($testProjects.Count)" -ForegroundColor Gray
Write-Host ""

$coverageFiles = @()
$testsPassed = $true

foreach ($project in $testProjects) {
    $projectName = (Get-Item $project).BaseName
    $coverageFile = Join-Path $coverageResults "$projectName.cobertura.xml"
    
    Write-Host "  📝 Testing: $projectName" -ForegroundColor White
    
    # Run tests with coverage
    dotnet test $project `
        --configuration Release `
        --no-build `
        --collect:"XPlat Code Coverage" `
        --results-directory $coverageResults `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ❌ Tests failed for $projectName" -ForegroundColor Red
        $testsPassed = $false
    } else {
        Write-Host "  ✅ Tests passed for $projectName" -ForegroundColor Green
    }
    
    # Find the generated coverage file
    $generatedCoverage = Get-ChildItem -Path $coverageResults -Recurse -Filter "coverage.cobertura.xml" | 
        Where-Object { $_.FullName -notlike "*$coverageFile*" } |
        Select-Object -First 1
    
    if ($generatedCoverage) {
        # Rename to project-specific name
        Copy-Item $generatedCoverage.FullName $coverageFile -Force
        $coverageFiles += $coverageFile
        Write-Host "  📄 Coverage saved: $coverageFile" -ForegroundColor Gray
    } else {
        Write-Host "  ⚠️  Warning: No coverage file found for $projectName" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

if (-not $testsPassed) {
    Write-Host "❌ Some tests failed. Coverage report will still be generated." -ForegroundColor Red
    Write-Host ""
}

# Generate HTML report
Write-Host "📊 Generating HTML coverage report..." -ForegroundColor Cyan

if ($coverageFiles.Count -eq 0) {
    Write-Host "❌ No coverage files found. Cannot generate report." -ForegroundColor Red
    exit 1
}

Write-Host "Coverage files found: $($coverageFiles.Count)" -ForegroundColor Gray
$coverageFiles | ForEach-Object { Write-Host "  - $_" -ForegroundColor DarkGray }
Write-Host ""

$reportGenArgs = @(
    "-reports:$($coverageFiles -join ';')",
    "-targetdir:$coverageDir",
    "-reporttypes:Html;TextSummary;Badges",
    "-classfilters:-System.*;-Microsoft.*"
)

reportgenerator @reportGenArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Report generation failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Coverage report generated successfully!" -ForegroundColor Green
Write-Host ""

# Parse summary file for coverage percentage
$summaryFile = Join-Path $coverageDir "Summary.txt"
if (Test-Path $summaryFile) {
    $summary = Get-Content $summaryFile
    Write-Host "📈 Coverage Summary:" -ForegroundColor Cyan
    Write-Host "===================" -ForegroundColor Cyan
    $summary | ForEach-Object { Write-Host $_ -ForegroundColor White }
    Write-Host ""
    
    # Extract line coverage percentage
    $lineCoverageLine = $summary | Where-Object { $_ -match "Line coverage:\s*([\d.]+)%" }
    if ($lineCoverageLine -match "([\d.]+)%") {
        $actualCoverage = [decimal]$matches[1]
        
        Write-Host "Coverage threshold: $MinimumCoverage%" -ForegroundColor Gray
        Write-Host "Actual coverage: $actualCoverage%" -ForegroundColor $(if ($actualCoverage -ge $MinimumCoverage) { "Green" } else { "Yellow" })
        Write-Host ""
        
        if ($actualCoverage -lt $MinimumCoverage) {
            Write-Host "⚠️  WARNING: Coverage is below the $MinimumCoverage% threshold!" -ForegroundColor Yellow
            Write-Host "Current: $actualCoverage% | Target: $MinimumCoverage% | Gap: $($MinimumCoverage - $actualCoverage)%" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "💡 To increase coverage:" -ForegroundColor Cyan
            Write-Host "  1. Add unit tests for untested business logic" -ForegroundColor Gray
            Write-Host "  2. Add integration tests for API endpoints" -ForegroundColor Gray
            Write-Host "  3. Add component tests for Blazor components" -ForegroundColor Gray
            Write-Host ""
            
            # Note: Not failing the script, just warning
            # Uncomment the line below to enforce coverage threshold:
            # exit 1
        } else {
            Write-Host "✅ Coverage meets the $MinimumCoverage% threshold!" -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "📂 Coverage report location:" -ForegroundColor Cyan
Write-Host "  $reportHtml" -ForegroundColor White
Write-Host ""

# Open report in browser
if (-not $NoOpen) {
    Write-Host "🌐 Opening coverage report in browser..." -ForegroundColor Cyan
    Start-Process $reportHtml
} else {
    Write-Host "💡 Open report manually:" -ForegroundColor Cyan
    Write-Host "  Start-Process '$reportHtml'" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✅ Coverage generation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review coverage gaps in the HTML report" -ForegroundColor Gray
Write-Host "  2. Add tests for uncovered code paths" -ForegroundColor Gray
Write-Host "  3. Re-run this script to verify improvements" -ForegroundColor Gray
Write-Host "  4. Commit docs/coverage/index.html for CI verification" -ForegroundColor Gray

exit 0
