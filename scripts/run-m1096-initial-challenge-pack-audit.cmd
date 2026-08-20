@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.6.4 initial operational challenge-pack audit...
echo.
echo This gate validates six versioned Application-layer challenge packs composed only from existing scenario/check/fault owners.
echo External demand remains evidence only, score arithmetic remains M10.9.6.3-owned, and the pack layer has no plant command authority.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Packs.M10964InitialOperationalChallengePackTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.6.4 artifact summary ===
if exist "artifacts\m1096-initial-challenge-packs\01-m1096-initial-operational-challenge-pack.summary.txt" (
  type "artifacts\m1096-initial-challenge-packs\01-m1096-initial-operational-challenge-pack.summary.txt"
) else (
  echo ERROR: expected M10.9.6.4 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.6.4 initial operational challenge-pack audit completed.
exit /b 0
