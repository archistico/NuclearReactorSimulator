@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "APP_PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"

echo Running M10.9.8.2 Hotfix 1 Healthy Assistance x Authority / mission / F4 robustness audit...
echo.
echo Scope: execute HAA-01..HAA-09 on production-safe bounded-demand @2, preserve historical @1 exactly,
echo cross the reported control-out failure region on exact-v4, requalify F4, and audit every selectable/interactive list refresh seam.
echo No Simulation coefficient/physics, protection, scoring, archive schema or fingerprint algorithm change.
echo.

if exist "artifacts\m1098-healthy-assistance-authority-matrix" rd /s /q "artifacts\m1098-healthy-assistance-authority-matrix"

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for M10.9.8 matrix-v1/matrix-v2 validation.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10981-integrated-validation-matrix.ps1" -RepositoryRoot "%CD%" -HistoricalReuse
if errorlevel 1 exit /b 1

powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10982-integrated-validation-matrix-v2.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Packs.M10982Hotfix1ProductionMissionPackTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10982HealthyAssistanceAuthorityMatrixTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.TrainingAssistanceAuthorityIndependenceTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.PlantControlAuthorityIntegrationTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.M10961ChallengeLifecycleContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Demand.M10962ExternalEnergyDemandProfileTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Scoring.M10963ChallengeScoringContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Recording.ScenarioAutomationReplayTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.Runtime.DesktopHostFailurePolicyTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ViewModels.OperatorComputerM104CommandConsoleTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ViewModels.M10982Hotfix1Rev5ListRefreshStabilityTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.Views.ControlRoomComputerControlXamlContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceWorkspaceUiTests" --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.8.2 Hotfix 1 artifact summary ===
if exist "artifacts\m1098-healthy-assistance-authority-matrix\01-m10982-healthy-assistance-authority-matrix.summary.txt" (
  type "artifacts\m1098-healthy-assistance-authority-matrix\01-m10982-healthy-assistance-authority-matrix.summary.txt"
) else (
  echo ERROR: expected M10.9.8.2 Hotfix 1 summary artifact was not written.
  exit /b 1
)

if exist "artifacts\m1098-healthy-assistance-authority-matrix\02-m10982-rev5-interactive-list-stability.summary.txt" (
  echo.
  type "artifacts\m1098-healthy-assistance-authority-matrix\02-m10982-rev5-interactive-list-stability.summary.txt"
) else (
  echo ERROR: expected M10.9.8.2 REV5 interactive-list stability artifact was not written.
  exit /b 1
)

echo.
echo M10.9.8.2 Hotfix 1 REV5 automated healthy matrix / production mission / F4 / interactive-list audit completed.
echo Manual smoke validation: run bounded-demand-following-5-10-5@2 beyond STEP 1000 and exercise command catalog, dependency chain, session checkpoints, target selectors, MISSION timeline hover and ENTER dispatch.
exit /b 0
