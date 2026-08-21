@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.7.1 immutable mission/performance presentation-contract audit...
echo.
echo This gate projects M10.9.6 lifecycle, demand and score truth plus existing assistance/control-authority state into a read-only presentation contract.
echo It does not choose workstation placement, modify the Operator Computer F1-F8 contract, add UI, scoring arithmetic or plant command authority.
echo.

if exist "artifacts\m1097-mission-performance-contract" rd /s /q "artifacts\m1097-mission-performance-contract"
dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10971MissionPerformancePresentationContractTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.1 artifact summary ===
if exist "artifacts\m1097-mission-performance-contract\01-m10971-mission-performance-presentation-contract.summary.txt" (
  type "artifacts\m1097-mission-performance-contract\01-m10971-mission-performance-presentation-contract.summary.txt"
) else (
  echo ERROR: expected M10.9.7.1 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.1 presentation-contract audit completed.
echo If build, ordinary tests and this focused gate are green, validate M10.9.7.1 before making the explicit M10.9.7.2 navigation decision.
exit /b 0
