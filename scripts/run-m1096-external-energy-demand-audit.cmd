@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.6.2 deterministic external energy-demand profile audit...
echo.
echo This gate validates Application-layer training demand evidence only.
echo External demand remains distinct from requested generator load and actual electrical output.
echo It introduces no score arithmetic, UI, generator-request mutation, grid coupling, supervisory authority or physics change.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Demand.M10962ExternalEnergyDemandProfileTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.6.2 artifact summary ===
if exist "artifacts\m1096-external-energy-demand\01-m1096-external-energy-demand-profile-contract.summary.txt" (
  type "artifacts\m1096-external-energy-demand\01-m1096-external-energy-demand-profile-contract.summary.txt"
) else (
  echo ERROR: expected M10.9.6.2 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.6.2 deterministic external energy-demand profile audit completed.
exit /b 0
