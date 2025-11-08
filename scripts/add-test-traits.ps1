<#
.SYNOPSIS
    Adds xUnit Trait attributes to test methods for better test organization
.DESCRIPTION
    This script automatically adds [Trait("Category", "...")] attributes to all test methods
    based on their project location:
    - Core.Tests -> Unit
    - Api.Tests -> Integration
    - E2E.Tests -> E2E
.EXAMPLE
    .\scripts\add-test-traits.ps1
.EXAMPLE
    .\scripts\add-test-traits.ps1 -DryRun
#>

[CmdletBinding()]
param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host "🏷️  xUnit Trait Attribute Adder" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray

# Test file mappings
$testProjects = @{
    "Core.Tests" = @{
        Category = "Unit"
        Path = "backend/tests/Po.PoDropSquare.Core.Tests"
    }
    "Api.Tests" = @{
        Category = "Integration"
        Path = "backend/tests/Po.PoDropSquare.Api.Tests"
    }
    "Blazor.Tests" = @{
        Category = "Component"
        Path = "frontend/tests/Po.PoDropSquare.Blazor.Tests"
    }
    "E2E.Tests" = @{
        Category = "E2E"
        Path = "backend/tests/Po.PoDropSquare.E2E.Tests"
    }
}

$totalFiles = 0
$totalTests = 0
$modifiedFiles = 0

foreach ($projectName in $testProjects.Keys) {
    $config = $testProjects[$projectName]
    $projectPath = Join-Path $PSScriptRoot ".." $config.Path
    
    if (-not (Test-Path $projectPath)) {
        Write-Host "⚠️  Skipping $projectName - path not found: $projectPath" -ForegroundColor Yellow
        continue
    }
    
    Write-Host "`n📁 Processing $projectName ($($config.Category) tests)..." -ForegroundColor Yellow
    
    $testFiles = Get-ChildItem -Path $projectPath -Filter "*.cs" -Recurse | Where-Object {
        $_.Name -notlike "*.g.cs" -and $_.Name -notlike "*.designer.cs"
    }
    
    foreach ($file in $testFiles) {
        $totalFiles++
        $content = Get-Content $file.FullName -Raw
        $fileModified = $false
        $testsInFile = 0
        
        # Pattern to match test methods without Trait attribute
        # Matches: [Fact] or [Theory] followed by optional attributes, then method signature
        $pattern = '(?<indent>\s*)(?<attribute>\[(?:Fact|Theory)\])(?<after>[^\r\n]*(?:\r?\n(?!\s*\[Trait))*?\s*public\s+(?:async\s+)?(?:Task|void)\s+\w+)'
        
        $regexMatches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
        
        if ($regexMatches.Count -gt 0) {
            # Process matches in reverse order to preserve positions
            for ($i = $regexMatches.Count - 1; $i -ge 0; $i--) {
                $match = $regexMatches[$i]
                
                # Check if already has Trait attribute
                $afterAttr = $content.Substring($match.Index, $match.Length)
                
                # Check the lines after [Fact]/[Theory] for existing [Trait]
                if ($afterAttr -match '\[Trait\(') {
                    continue  # Already has Trait
                }
                
                # Add Trait attribute
                $indent = $match.Groups['indent'].Value
                $attribute = $match.Groups['attribute'].Value
                $after = $match.Groups['after'].Value
                
                # Determine additional traits based on file name
                $additionalTraits = ""
                if ($file.Name -match '(\w+)Tests\.cs$') {
                    $featureName = $Matches[1]
                    if ($featureName -ne "Unit" -and $featureName -ne "E2E") {
                        $additionalTraits = "`n$indent[Trait(`"Feature`", `"$featureName`")]"
                    }
                }
                
                $replacement = "$indent$attribute`n$indent[Trait(`"Category`", `"$($config.Category)`")]$additionalTraits$after"
                
                $content = $content.Substring(0, $match.Index) + $replacement + $content.Substring($match.Index + $match.Length)
                
                $fileModified = $true
                $testsInFile++
            }
            
            if ($fileModified) {
                $modifiedFiles++
                $totalTests += $testsInFile
                
                Write-Host "  ✏️  $($file.Name): Added traits to $testsInFile test(s)" -ForegroundColor Green
                
                if (-not $DryRun) {
                    Set-Content -Path $file.FullName -Value $content -NoNewline
                }
            }
        }
    }
}

# Summary
Write-Host "`n" + ("=" * 60) -ForegroundColor Gray
Write-Host "📊 Summary" -ForegroundColor Cyan
Write-Host "  Files scanned: $totalFiles" -ForegroundColor Gray
Write-Host "  Files modified: $modifiedFiles" -ForegroundColor Green
Write-Host "  Test methods updated: $totalTests" -ForegroundColor Green

if ($DryRun) {
    Write-Host "`n⚠️  DRY RUN - No files were actually modified" -ForegroundColor Yellow
    Write-Host "  Run without -DryRun to apply changes" -ForegroundColor Yellow
} else {
    Write-Host "`n✅ Traits added successfully!" -ForegroundColor Green
    Write-Host "`nVerify changes:" -ForegroundColor Cyan
    Write-Host "  git diff backend/tests/" -ForegroundColor White
    Write-Host "`nRun tests by category:" -ForegroundColor Cyan
    Write-Host "  dotnet test --filter `"Category=Unit`"" -ForegroundColor White
    Write-Host "  dotnet test --filter `"Category=Integration`"" -ForegroundColor White
    Write-Host "  dotnet test --filter `"Category=E2E`"" -ForegroundColor White
}

Write-Host ("=" * 60) -ForegroundColor Gray
