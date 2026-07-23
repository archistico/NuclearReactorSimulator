$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj'
dotnet test --project $project --no-build -- --explicit only --filter-trait "Category=OperationalEnvelopeAudit" --parallel none
exit $LASTEXITCODE
