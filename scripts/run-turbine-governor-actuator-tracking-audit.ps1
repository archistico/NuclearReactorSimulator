$ErrorActionPreference = 'Stop'
$repoRoot = Join-Path $PSScriptRoot '..'
$testExecutable = Join-Path $repoRoot 'tests/NuclearReactorSimulator.Application.Tests/bin/Debug/net10.0/NuclearReactorSimulator.Application.Tests.exe'
if (-not (Test-Path -LiteralPath $testExecutable)) {
    throw 'D.3 audit test executable not found. Run: dotnet build tests/NuclearReactorSimulator.Application.Tests/NuclearReactorSimulator.Application.Tests.csproj'
}
& $testExecutable -trait 'Category=TurbineGovernorActuatorTrackingAudit' -explicit only -parallel none -showLiveOutput -reporter verbose
exit $LASTEXITCODE