$ErrorActionPreference = 'Stop'

Write-Host 'Restoring packages...'
dotnet restore

Write-Host 'Building solution...'
dotnet build --no-restore

Write-Host 'Running tests...'
dotnet test --no-build

Write-Host 'Verification completed successfully.'
