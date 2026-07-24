$ErrorActionPreference = "Stop"
dotnet test --project "$PSScriptRoot/../tests/NuclearReactorSimulator.Application.Tests/NuclearReactorSimulator.Application.Tests.csproj" --no-build -- --explicit only --filter-trait "Category=TurbineAdmissionAuthorityAudit" --parallel none
exit $LASTEXITCODE
