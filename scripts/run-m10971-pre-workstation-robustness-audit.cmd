@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APP_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "INFRA_PROJECT=tests\NuclearReactorSimulator.Infrastructure.Tests\NuclearReactorSimulator.Infrastructure.Tests.csproj"

echo Running M10.9.7.1 Hotfix 2 pre-workstation presentation/archive robustness audit...
echo.
echo This gate hardens the validated 7.1 read model before any workstation/navigation candidate is promoted.
echo It covers terminal logical-step alignment, objective metadata, future-event filtering/bounding,
echo shared requested-load evidence and malformed session-archive handling.
echo It adds no workstation placement, scoring formula, challenge definition, plant command authority or physics change.
echo.

if exist "artifacts\m10971-hotfix2-pre-workstation-robustness" rd /s /q "artifacts\m10971-hotfix2-pre-workstation-robustness"

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10971MissionPerformancePresentationContractTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10971Hotfix2PreWorkstationRobustnessTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%INFRA_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Infrastructure.Tests.Scenarios.Recording.JsonScenarioSessionArchiveSerializerTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.1 Hotfix 2 artifact summary ===
if exist "artifacts\m10971-hotfix2-pre-workstation-robustness\01-m10971-hotfix2-pre-workstation-robustness.summary.txt" (
  type "artifacts\m10971-hotfix2-pre-workstation-robustness\01-m10971-hotfix2-pre-workstation-robustness.summary.txt"
) else (
  echo ERROR: expected M10.9.7.1 Hotfix 2 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.1 Hotfix 2 pre-workstation robustness audit completed.
echo If build, ordinary tests and this focused gate are green, validate Hotfix 2 and rebuild M10.9.7.2 from that baseline.
exit /b 0
