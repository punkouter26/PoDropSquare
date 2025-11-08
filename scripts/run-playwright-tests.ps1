# Quick Run Playwright Tests
# This script runs the Playwright TypeScript E2E tests

Write-Host "🎭 PoDropSquare - Playwright E2E Test Runner" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to playwright directory
$playwrightDir = Join-Path $PSScriptRoot ".." "tests" "playwright"

if (-not (Test-Path $playwrightDir)) {
    Write-Host "❌ Error: Playwright directory not found at: $playwrightDir" -ForegroundColor Red
    exit 1
}

Set-Location $playwrightDir

# Check if node_modules exists
if (-not (Test-Path "node_modules")) {
    Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
    npm install
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ npm install failed" -ForegroundColor Red
        exit 1
    }
}

# Check if Playwright browsers are installed
Write-Host "🔍 Checking Playwright browsers..." -ForegroundColor Cyan
$browsersInstalled = Test-Path "$env:USERPROFILE\.cache\ms-playwright\chromium-*"

if (-not $browsersInstalled) {
    Write-Host "📥 Installing Playwright browsers..." -ForegroundColor Yellow
    npx playwright install chromium
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Browser installation failed" -ForegroundColor Red
        exit 1
    }
}

# Parse command line arguments
$testCommand = "npm test"
$extraArgs = ""

if ($args.Count -gt 0) {
    switch ($args[0]) {
        "headed" {
            Write-Host "🌐 Running tests with visible browser..." -ForegroundColor Green
            $testCommand = "npm run test:headed"
        }
        "debug" {
            Write-Host "🐛 Running tests in debug mode..." -ForegroundColor Green
            $testCommand = "npm run test:debug"
        }
        "ui" {
            Write-Host "🎨 Opening Playwright UI mode..." -ForegroundColor Green
            $testCommand = "npm run test:ui"
        }
        "chromium" {
            Write-Host "🌐 Running tests on Chromium only..." -ForegroundColor Green
            $testCommand = "npm run test:chromium"
        }
        "mobile" {
            Write-Host "📱 Running mobile tests..." -ForegroundColor Green
            $testCommand = "npm run test:mobile"
        }
        "accessibility" {
            Write-Host "♿ Running accessibility tests..." -ForegroundColor Green
            $testCommand = "npm run test:accessibility"
        }
        "report" {
            Write-Host "📊 Opening test report..." -ForegroundColor Green
            $testCommand = "npm run show-report"
        }
        default {
            Write-Host "⚡ Running tests: $($args[0])" -ForegroundColor Green
            $extraArgs = $args[0]
            $testCommand = "npx playwright test $extraArgs"
        }
    }
} else {
    Write-Host "⚡ Running all tests..." -ForegroundColor Green
}

Write-Host ""
Write-Host "Command: $testCommand $extraArgs" -ForegroundColor DarkGray
Write-Host ""

# Run tests
Invoke-Expression $testCommand

$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "✅ Tests completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "💡 View report: npm run show-report" -ForegroundColor Cyan
} else {
    Write-Host "❌ Tests failed (exit code: $exitCode)" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔍 Debug options:" -ForegroundColor Yellow
    Write-Host "  - Run with visible browser: .\run-playwright-tests.ps1 headed" -ForegroundColor Gray
    Write-Host "  - Debug mode: .\run-playwright-tests.ps1 debug" -ForegroundColor Gray
    Write-Host "  - View report: .\run-playwright-tests.ps1 report" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Available commands:" -ForegroundColor Cyan
Write-Host "  .\run-playwright-tests.ps1           # Run all tests" -ForegroundColor Gray
Write-Host "  .\run-playwright-tests.ps1 headed    # Run with visible browser" -ForegroundColor Gray
Write-Host "  .\run-playwright-tests.ps1 debug     # Debug mode" -ForegroundColor Gray
Write-Host "  .\run-playwright-tests.ps1 ui        # Interactive UI mode" -ForegroundColor Gray
Write-Host "  .\run-playwright-tests.ps1 chromium  # Chromium only" -ForegroundColor Gray
Write-Host "  .\run-playwright-tests.ps1 mobile    # Mobile tests" -ForegroundColor Gray
Write-Host "  .\run-playwright-tests.ps1 accessibility  # Accessibility tests" -ForegroundColor Gray
Write-Host "  .\run-playwright-tests.ps1 report    # Show test report" -ForegroundColor Gray

exit $exitCode
