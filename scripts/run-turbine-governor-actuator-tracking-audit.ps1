$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj'
dotnet test --project $project --no-build -- --explicit only --filter-trait 'Category=TurbineGovernorActuatorTrackingAudit' --parallel none
exit $LASTEXITCODE
