@echo off
setlocal EnableExtensions
set "HISTORICAL_REUSE=0"
if /I "%~1"=="--historical-reuse" set "HISTORICAL_REUSE=1"
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "APP_PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"

echo Running M10.9.7.4 deterministic Mission/Performance timeline audit...
echo.
echo Scope: fingerprint-v1 anchor, lifecycle-spine retention, deterministic timeline/drill-down and replay/checkpoint/archive-restored mission equivalence.
echo No Simulation physics, challenge/scoring/protection authority, archive-schema or plant-command-authority change.
echo.

if exist "artifacts\m10974-mission-performance-timeline" rd /s /q "artifacts\m10974-mission-performance-timeline"

findstr /C:"63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362" "tests\NuclearReactorSimulator.Application.Tests\ControlRoom\MissionPerformance\M10974FingerprintV1SchemaAnchorTests.cs" >nul
if errorlevel 1 (
  echo ERROR: fingerprint-v1 populated golden anchor is missing.
  exit /b 1
)
findstr /C:"63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362" "eng\frozen-evidence\ordinary\H29_ValidatedProductionActivationCandidateSummary.txt" >nul
if errorlevel 1 (
  echo ERROR: fingerprint-v1 golden anchor is not backed by retained H29 evidence.
  exit /b 1
)
findstr /C:"public const int CurrentSchemaVersion = 1;" "src\NuclearReactorSimulator.Application\Scenarios\Recording\ScenarioSessionArchive.cs" >nul
if errorlevel 1 (
  echo ERROR: session archive schema v1 contract changed unexpectedly.
  exit /b 1
)
findstr /C:"MaximumLifecycleSpineEntries = 32" "src\NuclearReactorSimulator.Application\ControlRoom\MissionPerformance\MissionPerformanceTimelineProjector.cs" >nul
if errorlevel 1 (
  echo ERROR: protected lifecycle-spine retention contract is missing.
  exit /b 1
)
findstr /C:"MaximumRecentOperationalEvidenceEntries = 100" "src\NuclearReactorSimulator.Application\ControlRoom\MissionPerformance\MissionPerformanceTimelineProjector.cs" >nul
if errorlevel 1 (
  echo ERROR: bounded recent operational evidence contract is missing.
  exit /b 1
)
findstr /C:"ReplayedChallengeContinuationEvidenceSource" "src\NuclearReactorSimulator.Application\ControlRoom\MissionPerformance\MissionPerformanceLiveSnapshotSource.cs" >nul
if errorlevel 1 (
  echo ERROR: verified replay-prefix to live challenge continuation seam is missing.
  exit /b 1
)
findstr /C:"missionPack is null" "src\NuclearReactorSimulator.App\Composition\CompositionRoot.cs" >nul
if errorlevel 1 (
  echo ERROR: explicit archive mission-binding boundary is missing.
  exit /b 1
)
findstr /C:"does not match archive scenario/initial-condition identity" "src\NuclearReactorSimulator.App\Composition\CompositionRoot.cs" >nul
if errorlevel 1 (
  echo ERROR: mismatched explicit archive mission pack does not fail closed.
  exit /b 1
)
findstr /C:"DETERMINISTIC TIMELINE / DRILL-DOWN" "src\NuclearReactorSimulator.App\Views\MainWindow.axaml" >nul
if errorlevel 1 (
  echo ERROR: deterministic Mission/Performance timeline UI is missing.
  exit /b 1
)
findstr /C:"Gesture=\"F9\"" "src\NuclearReactorSimulator.App\Views\MainWindow.axaml" >nul
if not errorlevel 1 (
  echo ERROR: F9 must remain absent.
  exit /b 1
)

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974FingerprintV1SchemaAnchorTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceTimelineContractTests" ^
  --parallel none
if errorlevel 1 exit /b 1

if "%HISTORICAL_REUSE%"=="1" (
  echo M10.9.7.4 exact-candidate descriptor check skipped in historical-reuse mode; current fingerprint/timeline/archive/UI tests still rerun.
) else (
  dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests.Current_DescribesM10974DeterministicMissionTimelineCandidate" ^
    --parallel none
  if errorlevel 1 exit /b 1
)

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceArchiveRestoreTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceDrillDownUiTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceWorkspaceUiTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.M10974MissionPerformanceTimelineAuditTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.4 artifact summary ===
if exist "artifacts\m10974-mission-performance-timeline\01-m10974-mission-performance-timeline.summary.txt" (
  type "artifacts\m10974-mission-performance-timeline\01-m10974-mission-performance-timeline.summary.txt"
) else (
  echo ERROR: expected M10.9.7.4 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.4 automated deterministic Mission/Performance timeline audit completed.
echo Final promotion also requires docs\M10_9_7_4_MANUAL_VALIDATION_CHECKLIST.md.
exit /b 0
