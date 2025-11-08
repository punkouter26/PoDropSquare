<#
.SYNOPSIS
    Stops local development environment for PoDropSquare
.DESCRIPTION
    This script stops Azurite and any running API instances.
.EXAMPLE
    .\scripts\stop-local-dev.ps1
.EXAMPLE
    .\scripts\stop-local-dev.ps1 -KeepAzurite
#>

[CmdletBinding()]
param(
    [switch]$KeepAzurite
)

Write-Host "🛑 Stopping PoDropSquare Local Development Environment" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray

# Stop API (dotnet run processes)
Write-Host "`n🔍 Looking for running API processes..." -ForegroundColor Yellow

$apiProcesses = Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {
    $_.MainWindowTitle -like "*Po.PoDropSquare.Api*" -or
    $_.Path -like "*Po.PoDropSquare.Api*"
}

if ($apiProcesses) {
    Write-Host "  ⏳ Stopping $($apiProcesses.Count) API process(es)..." -ForegroundColor Yellow
    $apiProcesses | Stop-Process -Force
    Write-Host "  ✅ API processes stopped" -ForegroundColor Green
} else {
    Write-Host "  ℹ️  No API processes found" -ForegroundColor Gray
}

# Stop Azurite (unless keeping it running)
if (-not $KeepAzurite) {
    Write-Host "`n🗄️  Looking for Azurite..." -ForegroundColor Yellow
    
    $azuriteProcesses = Get-Process azurite -ErrorAction SilentlyContinue
    
    if ($azuriteProcesses) {
        Write-Host "  ⏳ Stopping Azurite (PID: $($azuriteProcesses.Id))..." -ForegroundColor Yellow
        $azuriteProcesses | Stop-Process -Force
        Write-Host "  ✅ Azurite stopped" -ForegroundColor Green
    } else {
        Write-Host "  ℹ️  Azurite not running" -ForegroundColor Gray
    }
} else {
    Write-Host "`n🗄️  Keeping Azurite running (--KeepAzurite specified)" -ForegroundColor Yellow
}

Write-Host "`n✅ Local development environment stopped" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Gray
