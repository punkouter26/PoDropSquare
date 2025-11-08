<#
.SYNOPSIS
    Tests local development environment setup for PoDropSquare
.DESCRIPTION
    This script verifies that Azurite and the API are running correctly and can communicate.
.EXAMPLE
    .\scripts\test-local-setup.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Continue"

Write-Host "🧪 PoDropSquare Local Setup Verification" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray

$allTestsPassed = $true

# Test 1: Check Azurite is running
Write-Host "`n📋 Test 1: Azurite Process" -ForegroundColor Yellow
$azuriteProcess = Get-Process azurite -ErrorAction SilentlyContinue
if ($azuriteProcess) {
    Write-Host "  ✅ PASS: Azurite is running (PID: $($azuriteProcess.Id))" -ForegroundColor Green
} else {
    Write-Host "  ❌ FAIL: Azurite is not running" -ForegroundColor Red
    Write-Host "     Start with: azurite --silent --location ." -ForegroundColor Yellow
    $allTestsPassed = $false
}

# Test 2: Check Azurite Table Storage endpoint
Write-Host "`n📋 Test 2: Azurite Table Storage Endpoint" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:10002/devstoreaccount1/Tables" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✅ PASS: Table Storage endpoint responding (HTTP $($response.StatusCode))" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️  WARN: Unexpected status code: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ❌ FAIL: Cannot connect to Table Storage endpoint" -ForegroundColor Red
    Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Red
    $allTestsPassed = $false
}

# Test 3: Check Azurite Blob Storage endpoint
Write-Host "`n📋 Test 3: Azurite Blob Storage Endpoint" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:10000/devstoreaccount1?comp=list" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✅ PASS: Blob Storage endpoint responding (HTTP $($response.StatusCode))" -ForegroundColor Green
    }
} catch {
    Write-Host "  ❌ FAIL: Cannot connect to Blob Storage endpoint" -ForegroundColor Red
    Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Red
    $allTestsPassed = $false
}

# Test 4: Check API is running
Write-Host "`n📋 Test 4: API Server" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/health" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✅ PASS: API responding (HTTP $($response.StatusCode))" -ForegroundColor Green
        
        # Parse health response
        $healthData = $response.Content | ConvertFrom-Json
        Write-Host "     Status: $($healthData.status)" -ForegroundColor Gray
        
        # Check storage health
        if ($healthData.entries -and $healthData.entries.'Azure Table Storage') {
            $storageHealth = $healthData.entries.'Azure Table Storage'.status
            if ($storageHealth -eq 'Healthy') {
                Write-Host "  ✅ PASS: Storage connection healthy" -ForegroundColor Green
            } else {
                Write-Host "  ⚠️  WARN: Storage health: $storageHealth" -ForegroundColor Yellow
            }
        }
    }
} catch {
    Write-Host "  ❌ FAIL: Cannot connect to API" -ForegroundColor Red
    Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "     Is the API running? Start with: dotnet run --project backend/src/Po.PoDropSquare.Api" -ForegroundColor Yellow
    $allTestsPassed = $false
}

# Test 5: Check diagnostics page
Write-Host "`n📋 Test 5: Diagnostics Page" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/diag" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200 -and $response.Content -match "Diagnostics") {
        Write-Host "  ✅ PASS: Diagnostics page accessible" -ForegroundColor Green
    }
} catch {
    Write-Host "  ⚠️  WARN: Diagnostics page not accessible" -ForegroundColor Yellow
    Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Test 6: Submit test score
Write-Host "`n📋 Test 6: Score Submission API" -ForegroundColor Yellow
try {
    $testScore = @{
        playerName = "TEST"
        score = 9999
        timestamp = (Get-Date).ToString("o")
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/scores" -Method POST -Body $testScore -ContentType "application/json" -ErrorAction Stop
    
    Write-Host "  ✅ PASS: Score submitted successfully" -ForegroundColor Green
    Write-Host "     Score ID: $($response.scoreId)" -ForegroundColor Gray
} catch {
    Write-Host "  ⚠️  WARN: Score submission failed" -ForegroundColor Yellow
    Write-Host "     Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "     This might be due to rate limiting or validation" -ForegroundColor Gray
}

# Test 7: Check log files
Write-Host "`n📋 Test 7: Log Files" -ForegroundColor Yellow
$logDir = Join-Path $PSScriptRoot ".." "backend" "src" "Po.PoDropSquare.Api" "logs"
if (Test-Path $logDir) {
    $logFiles = Get-ChildItem $logDir -Filter "podropsquare-*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($logFiles) {
        Write-Host "  ✅ PASS: Log files found" -ForegroundColor Green
        Write-Host "     Latest: $($logFiles.Name) ($('{0:N2}' -f ($logFiles.Length / 1KB)) KB)" -ForegroundColor Gray
    } else {
        Write-Host "  ⚠️  WARN: No log files found" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠️  WARN: Log directory not found" -ForegroundColor Yellow
}

# Summary
Write-Host "`n" + ("=" * 60) -ForegroundColor Gray
if ($allTestsPassed) {
    Write-Host "✅ All critical tests passed! Local setup is healthy." -ForegroundColor Green
    Write-Host "`nYou can now:" -ForegroundColor Cyan
    Write-Host "  • View the app: http://localhost:5000" -ForegroundColor White
    Write-Host "  • Check diagnostics: http://localhost:5000/diag" -ForegroundColor White
    Write-Host "  • Run tests: dotnet test --filter `"FullyQualifiedName!~E2E`"" -ForegroundColor White
} else {
    Write-Host "⚠️  Some tests failed. Please check the errors above." -ForegroundColor Yellow
    Write-Host "`nQuick fixes:" -ForegroundColor Cyan
    Write-Host "  • Start Azurite: azurite --silent --location ." -ForegroundColor White
    Write-Host "  • Start API: dotnet run --project backend/src/Po.PoDropSquare.Api" -ForegroundColor White
    Write-Host "  • Or use: .\scripts\start-local-dev.ps1" -ForegroundColor White
}
Write-Host ("=" * 60) -ForegroundColor Gray
