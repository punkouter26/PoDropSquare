# PowerShell script to update all NuGet packages to .NET 9 versions
$projects = @(
    "backend\src\Po.PoDropSquare.Data",
    "backend\src\Po.PoDropSquare.Services",
    "frontend\src\Po.PoDropSquare.Blazor",
    "backend\tests\Po.PoDropSquare.Api.Tests",
    "backend\tests\Po.PoDropSquare.Core.Tests",
    "backend\tests\Po.PoDropSquare.E2E.Tests",
    "frontend\tests\Po.PoDropSquare.Blazor.Tests"
)

# Data project updates
Write-Host "Updating Po.PoDropSquare.Data packages..."
Set-Location "backend\src\Po.PoDropSquare.Data"
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 9.0.9

# Services project updates
Write-Host "Updating Po.PoDropSquare.Services packages..."
Set-Location "..\Po.PoDropSquare.Services"
dotnet add package Microsoft.Extensions.Caching.Memory --version 9.0.9
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 9.0.9

# Blazor project updates
Write-Host "Updating Po.PoDropSquare.Blazor packages..."
Set-Location "..\..\..\frontend\src\Po.PoDropSquare.Blazor"
dotnet add package Microsoft.AspNetCore.Components.WebAssembly --version 9.0.9
dotnet add package Microsoft.AspNetCore.Components.WebAssembly.DevServer --version 9.0.9

# Test projects updates
$testPackages = @(
    @{Name="coverlet.collector"; Version="6.0.4"},
    @{Name="Microsoft.NET.Test.Sdk"; Version="17.14.1"},
    @{Name="xunit"; Version="2.9.3"},
    @{Name="xunit.runner.visualstudio"; Version="3.1.4"}
)

foreach ($project in @("backend\tests\Po.PoDropSquare.Api.Tests", "backend\tests\Po.PoDropSquare.Core.Tests", "backend\tests\Po.PoDropSquare.E2E.Tests", "frontend\tests\Po.PoDropSquare.Blazor.Tests")) {
    Write-Host "Updating $project packages..."
    Set-Location "..\..\..\..\$project"
    foreach ($package in $testPackages) {
        dotnet add package $package.Name --version $package.Version
    }
}

# API Tests specific package
Set-Location "..\..\..\..\backend\tests\Po.PoDropSquare.Api.Tests"
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 9.0.9

Write-Host "All packages updated!"