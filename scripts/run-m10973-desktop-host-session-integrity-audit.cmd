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

echo Running M10.9.7.3 Hotfix 2 REV2 desktop host / session save integrity audit...
echo.
echo Scope: App/Application host integrity only. No Simulation physics, challenge/scoring/protection or archive-schema change.
echo.

if exist "artifacts\m10973-desktop-host-session-integrity" rd /s /q "artifacts\m10973-desktop-host-session-integrity"

findstr /C:"SetLength(0)" "src\NuclearReactorSimulator.App\Controls\ControlRoomComputerControl.axaml.cs" >nul
if not errorlevel 1 (
  echo ERROR: destructive truncate-first SAVE path is still present.
  exit /b 1
)
findstr /C:"TryGetLocalPath" "src\NuclearReactorSimulator.App\Controls\ControlRoomComputerControl.axaml.cs" >nul
if errorlevel 1 (
  echo ERROR: SAVE does not require an explicit local-filesystem path for safe replacement.
  exit /b 1
)
findstr /C:"File.Replace" "src\NuclearReactorSimulator.App\Persistence\DesktopSessionArchiveFileWriter.cs" >nul
if errorlevel 1 (
  echo ERROR: existing-file safe replacement contract is missing.
  exit /b 1
)
findstr /C:"DesktopHostFailurePolicy.IsExpectedRuntimeConstructionFailure" "src\NuclearReactorSimulator.App\Controls\ControlRoomComputerControl.axaml.cs" >nul
if errorlevel 1 (
  echo ERROR: START RECORDED SESSION is not using the shared runtime-construction failure policy.
  exit /b 1
)
findstr /C:"DesktopHostFailurePolicy.IsExpectedRuntimeConstructionFailure" "src\NuclearReactorSimulator.App\Views\MainWindow.axaml.cs" >nul
if errorlevel 1 (
  echo ERROR: RESET SESSION is not using the shared runtime-construction failure policy.
  exit /b 1
)
findstr /C:"DesktopHostFailurePolicy.IsExpectedArchiveOperationFailure" "src\NuclearReactorSimulator.App\Controls\ControlRoomComputerControl.axaml.cs" >nul
if errorlevel 1 (
  echo ERROR: archive operations are not using the shared expected-failure policy.
  exit /b 1
)
findstr /C:"or InvalidDataException" "src\NuclearReactorSimulator.App\Runtime\DesktopHostFailurePolicy.cs" >nul
if errorlevel 1 (
  echo ERROR: centralized archive failure policy does not explicitly classify InvalidDataException.
  exit /b 1
)
findstr /C:"replacementCommitted" "src\NuclearReactorSimulator.App\Persistence\DesktopSessionArchiveFileWriter.cs" >nul
if errorlevel 1 (
  echo ERROR: backup cleanup is not scoped to a successfully committed existing-file replacement.
  exit /b 1
)
findstr /C:"Gesture=\"F9\"" "src\NuclearReactorSimulator.App\Views\MainWindow.axaml" >nul
if not errorlevel 1 (
  echo ERROR: F9 must remain absent.
  exit /b 1
)

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.Runtime.DesktopControlRoomRuntimePumpTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.Runtime.DesktopHostFailurePolicyTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.Persistence.DesktopSessionArchiveFileWriterTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.Persistence.DesktopSessionArchiveSaveCoordinatorTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.Presentation.EngineeringNumberFormatterTests" ^
  --parallel none
if errorlevel 1 exit /b 1

if "%HISTORICAL_REUSE%"=="1" (
  echo M10.9.7.3 Hotfix 2 REV2 exact-candidate descriptor check skipped in historical-reuse mode; current host/session integrity tests still rerun.
) else (
  dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests.Current_DescribesM10973Hotfix2Rev2DesktopHostSessionIntegrityCandidate" ^
    --parallel none
  if errorlevel 1 exit /b 1
)

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceWorkspaceUiTests" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.M10973DesktopHostSessionIntegrityAuditTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.3 Hotfix 2 REV2 artifact summary ===
if exist "artifacts\m10973-desktop-host-session-integrity\01-m10973-desktop-host-session-integrity.summary.txt" (
  type "artifacts\m10973-desktop-host-session-integrity\01-m10973-desktop-host-session-integrity.summary.txt"
) else (
  echo ERROR: expected M10.9.7.3 Hotfix 2 REV2 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.3 Hotfix 2 REV2 automated desktop-host/session-integrity audit completed.
echo Final promotion also requires docs\M10_9_7_3_HOTFIX2_MANUAL_VALIDATION_CHECKLIST.md.
exit /b 0
