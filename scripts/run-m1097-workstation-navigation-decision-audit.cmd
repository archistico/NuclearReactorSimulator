@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"

echo Running M10.9.7.2 REV1 Mission/Performance workstation placement/navigation decision audit...
echo.
echo This gate freezes option A: a dedicated main-HMI Mission/Performance workspace with contextual navigation from COMPUTER.
echo It preserves the validated Operator Computer F1-F8 contract, adds no F9, does not yet activate the workspace UI, and adds no plant command authority.
echo.

if exist "artifacts\m1097-navigation-decision" rd /s /q "artifacts\m1097-navigation-decision"
dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10972MissionPerformanceNavigationDecisionTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.2 REV1 artifact summary ===
if exist "artifacts\m1097-navigation-decision\01-m10972-workstation-placement-navigation.summary.txt" (
  type "artifacts\m1097-navigation-decision\01-m10972-workstation-placement-navigation.summary.txt"
) else (
  echo ERROR: expected M10.9.7.2 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.2 REV1 placement/navigation decision audit completed.
echo If build, ordinary tests and this focused gate are green, validate M10.9.7.2 REV1 before addressing/qualifying pre-live hardening and activating the Mission/Performance workspace UI in M10.9.7.3.
exit /b 0
