@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.6.1 challenge lifecycle / logical-time contract audit...
echo.
echo This gate validates deterministic challenge lifecycle state only.
echo It introduces no external demand profile, score arithmetic, UI, plant command authority or physics change.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.M10961ChallengeLifecycleContractTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.6.1 artifact summary ===
if exist "artifacts\m1096-challenge-lifecycle\01-m1096-challenge-lifecycle-logical-time-contract.summary.txt" (
  type "artifacts\m1096-challenge-lifecycle\01-m1096-challenge-lifecycle-logical-time-contract.summary.txt"
) else (
  echo ERROR: expected M10.9.6.1 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.6.1 challenge lifecycle / logical-time contract audit completed.
exit /b 0
