# Start Azurite for PoDropSquare development
# This script starts the Azure Storage Emulator for local development

Write-Host "Starting Azurite for PoDropSquare development..." -ForegroundColor Green

# Create data directory if it doesn't exist
$dataDir = "c:\Users\punko\Downloads\PoDropSquare\azurite-data"
if (!(Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir
    Write-Host "Created Azurite data directory: $dataDir" -ForegroundColor Yellow
}

# Start Azurite with specific configuration
Write-Host "Starting Azurite on port 10002 for Table Storage..." -ForegroundColor Cyan
Write-Host "Connection string will be: DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;" -ForegroundColor Yellow

azurite --silent --location $dataDir --debug azurite-debug.log --tablePort 10002