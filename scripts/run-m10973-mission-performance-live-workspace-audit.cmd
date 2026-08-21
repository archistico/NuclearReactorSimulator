@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "APP_PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"

echo Running M10.9.7.3 Hotfix 1 REV2 live Mission / Performance workspace audit...
echo.
echo This gate activates the presentation route only. Challenge/scoring/protection/plant-command ownership remains unchanged.
echo Normal desktop startup remains mission-unbound; manual active-mission validation uses an explicit exact --mission-pack option.
echo.

if exist "artifacts\m10973-mission-performance-live-workspace" rd /s /q "artifacts\m10973-mission-performance-live-workspace"

findstr /C:"MissionPerformance = 7" "src\NuclearReactorSimulator.Application\ControlRoom\ControlRoomWorkspaceId.cs" >nul
if errorlevel 1 (
  echo ERROR: MISSION workspace identity is not live registered.
  exit /b 1
)
findstr /C:"Content=\"OPEN MISSION\"" "src\NuclearReactorSimulator.App\Views\MainWindow.axaml" >nul
if errorlevel 1 (
  echo ERROR: COMPUTER contextual MISSION navigation action is missing.
  exit /b 1
)
findstr /C:"Gesture=\"F9\"" "src\NuclearReactorSimulator.App\Views\MainWindow.axaml" >nul
if not errorlevel 1 (
  echo ERROR: F9 must not be introduced by M10.9.7.3 Hotfix 1 REV2.
  exit /b 1
)

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceLiveWiringTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests.Current_DescribesM10973Hotfix1Rev2LiveMissionPerformanceWorkspaceCandidate" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.App.Tests.Views.OperatorExperienceM1091HmiShellTests.SituationStrip_ExposesOperationalStatusWithoutInventingFutureDemandCapability" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceWorkspaceUiTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.3 Hotfix 1 REV2 artifact summary ===
if exist "artifacts\m10973-mission-performance-live-workspace\01-m10973-mission-performance-live-workspace.summary.txt" (
  type "artifacts\m10973-mission-performance-live-workspace\01-m10973-mission-performance-live-workspace.summary.txt"
) else (
  echo ERROR: expected M10.9.7.3 Hotfix 1 REV2 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.3 Hotfix 1 REV2 automated live-workspace audit completed.
echo Final promotion also requires docs\M10_9_7_3_MANUAL_VALIDATION_CHECKLIST.md.
exit /b 0
