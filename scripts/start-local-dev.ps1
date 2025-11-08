<#
.SYNOPSIS
    Starts local development environment for PoDropSquare
.DESCRIPTION
    This script starts Azurite (if not running) and the API project for local development.
    It verifies Azurite connectivity before starting the application.
.EXAMPLE
    .\scripts\start-local-dev.ps1
.EXAMPLE
    .\scripts\start-local-dev.ps1 -SkipAzurite
#>

[CmdletBinding()]
param(
    [switch]$SkipAzurite,
    [switch]$SkipBuild,
    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 PoDropSquare Local Development Startup" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray

# Check prerequisites
Write-Host "`n📋 Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "  ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  ❌ .NET SDK not found. Please install .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

# Check Azurite installation (if not skipping)
if (-not $SkipAzurite) {
    try {
        $azuriteVersion = azurite --version 2>&1 | Out-String
        if ($azuriteVersion -match "(\d+\.\d+\.\d+)") {
            Write-Host "  ✅ Azurite: $($Matches[1])" -ForegroundColor Green
        }
    } catch {
        Write-Host "  ⚠️  Azurite not installed. Install with: npm install -g azurite" -ForegroundColor Yellow
        Write-Host "  Continuing anyway (might fail if storage is needed)..." -ForegroundColor Yellow
    }
}

# Start Azurite if not already running
if (-not $SkipAzurite) {
    Write-Host "`n🗄️  Checking Azurite storage emulator..." -ForegroundColor Yellow
    
    $azuriteProcess = Get-Process azurite -ErrorAction SilentlyContinue
    
    if ($azuriteProcess) {
        Write-Host "  ✅ Azurite already running (PID: $($azuriteProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "  ⏳ Starting Azurite..." -ForegroundColor Yellow
        
        # Create azurite data directory if it doesn't exist
        $azuriteDir = Join-Path $PSScriptRoot ".." ".azurite"
        if (-not (Test-Path $azuriteDir)) {
            New-Item -ItemType Directory -Path $azuriteDir | Out-Null
        }
        
        # Start Azurite in background
        $azuriteArgs = @(
            "--silent"
            "--location", $azuriteDir
            "--debug", (Join-Path $azuriteDir "debug.log")
        )
        
        Start-Process "azurite" -ArgumentList $azuriteArgs -WindowStyle Hidden
        
        # Wait for Azurite to start
        Start-Sleep -Seconds 3
        
        # Verify it started
        $azuriteProcess = Get-Process azurite -ErrorAction SilentlyContinue
        if ($azuriteProcess) {
            Write-Host "  ✅ Azurite started (PID: $($azuriteProcess.Id))" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Failed to start Azurite" -ForegroundColor Red
            exit 1
        }
    }
    
    # Test Azurite connectivity
    Write-Host "  ⏳ Testing Azurite connectivity..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "http://127.0.0.1:10002/devstoreaccount1/Tables" -UseBasicParsing -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "  ✅ Azurite Table Storage responding (Port 10002)" -ForegroundColor Green
        }
    } catch {
        Write-Host "  ⚠️  Azurite connectivity test failed: $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "  Continuing anyway..." -ForegroundColor Yellow
    }
}

# Build the project (unless skipped)
if (-not $SkipBuild) {
    Write-Host "`n🔨 Building solution..." -ForegroundColor Yellow
    
    $slnPath = Join-Path $PSScriptRoot ".." "PoDropSquare.sln"
    
    dotnet build $slnPath --no-restore --configuration Debug
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ❌ Build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  ✅ Build succeeded" -ForegroundColor Green
}

# Start the API
Write-Host "`n🌐 Starting API server..." -ForegroundColor Yellow

$apiPath = Join-Path $PSScriptRoot ".." "backend" "src" "Po.PoDropSquare.Api"

Write-Host "  Project: $apiPath" -ForegroundColor Gray
Write-Host "  Endpoints:" -ForegroundColor Gray
Write-Host "    - HTTP:  http://localhost:5000" -ForegroundColor Cyan
Write-Host "    - HTTPS: https://localhost:5001" -ForegroundColor Cyan
Write-Host "  Health Check: http://localhost:5000/api/health" -ForegroundColor Gray
Write-Host "  Diagnostics:  http://localhost:5000/diag" -ForegroundColor Gray
Write-Host "" -ForegroundColor Gray
Write-Host "  Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host "=" * 60 -ForegroundColor Gray
Write-Host ""

# Open browser (unless disabled)
if (-not $NoBrowser) {
    Start-Sleep -Seconds 2
    Start-Process "http://localhost:5000"
}

# Run the API (this will block until Ctrl+C)
Set-Location $apiPath
dotnet run --no-build

Write-Host "`n👋 Server stopped" -ForegroundColor Yellow
