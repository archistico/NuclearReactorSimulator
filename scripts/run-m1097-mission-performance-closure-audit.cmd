@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "APP_PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"

echo Running M10.9.7.5 Hotfix 1 Mission / Performance closure audit...
echo.
echo Scope: cumulative closure matrix over M10.9.7.4 Hotfix 1 VALIDATED.
echo Hotfix 1 repairs only the Windows batch audit wrapper; it adds no production runtime semantics.
echo This gate adds no Simulation physics, scoring/challenge/protection owner, archive schema, plant-command authority or F9.
echo.

if exist "artifacts\m1097-mission-performance-closure" rd /s /q "artifacts\m1097-mission-performance-closure"

findstr /C:"M10.9.7.5 Hotfix 1" "src\NuclearReactorSimulator.Application\ApplicationDescriptor.cs" >nul
if errorlevel 1 (
  echo ERROR: application descriptor is not aligned to the M10.9.7.5 Hotfix 1 closure candidate.
  exit /b 1
)
findstr /C:"63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362" "tests\NuclearReactorSimulator.Application.Tests\ControlRoom\MissionPerformance\M10974FingerprintV1SchemaAnchorTests.cs" >nul
if errorlevel 1 (
  echo ERROR: frozen fingerprint-v1 golden anchor changed or is missing.
  exit /b 1
)
findstr /C:"public const int CurrentSchemaVersion = 1;" "src\NuclearReactorSimulator.Application\Scenarios\Recording\ScenarioSessionArchive.cs" >nul
if errorlevel 1 (
  echo ERROR: session archive schema v1 contract changed unexpectedly.
  exit /b 1
)
findstr /C:"Gesture=\"F9\"" "src\NuclearReactorSimulator.App\Views\MainWindow.axaml" >nul
if not errorlevel 1 (
  echo ERROR: F9 must remain absent at M10.9.7 closure.
  exit /b 1
)
findstr /C:"does not dispatch a plant command" "src\NuclearReactorSimulator.App\Views\MainWindow.axaml" >nul
if errorlevel 1 (
  echo ERROR: presentation-only Mission drill-down authority marker is missing.
  exit /b 1
)

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10975MissionPerformanceClosureContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10975Hotfix1ClosureAuditScriptContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10971MissionPerformancePresentationContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceLiveWiringTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceTimelineContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Replay.M10965ChallengeReplayCheckpointClosureTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests.Current_DescribesM10975MissionPerformanceClosureCandidate" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceWorkspaceUiTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceArchiveRestoreTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceDrillDownUiTests" --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.5 Hotfix 1 artifact summary ===
if exist "artifacts\m1097-mission-performance-closure\01-m10975-mission-performance-closure.summary.txt" (
  type "artifacts\m1097-mission-performance-closure\01-m10975-mission-performance-closure.summary.txt"
) else (
  echo ERROR: expected M10.9.7.5 closure summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.5 Hotfix 1 automated Mission / Performance closure audit completed.
echo Final M10.9.7 promotion also requires docs\M10_9_7_5_MANUAL_VALIDATION_CHECKLIST.md.
exit /b 0
