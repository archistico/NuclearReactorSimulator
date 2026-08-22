@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "APP_PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"

echo Running M10.9.8.5 integrated Human / Automation / HMI closure preflight...
echo.
echo Scope: revalidate the accepted M10.9.8 matrices and current operator-visible HMI contracts before the mandatory manual integrated acceptance.
echo This gate adds no production runtime, Simulation physics, archive schema, fingerprint algorithm, challenge/scoring/protection ownership or plant-command authority.
echo.

if exist "artifacts\m1098-integrated-hmi-closure" rd /s /q "artifacts\m1098-integrated-hmi-closure"
mkdir "artifacts\m1098-integrated-hmi-closure"
if errorlevel 1 exit /b 1

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for M10.9.8.5 contract validation.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10981-integrated-validation-matrix.ps1" -RepositoryRoot "%CD%" -HistoricalReuse
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10982-integrated-validation-matrix-v2.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10983-degraded-fault-protection-takeover-matrix.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10984-replay-checkpoint-same-seed-integrity.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10985-integrated-hmi-closure-contract.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10984ReplayCheckpointSameSeedIntegrityTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10983DegradedFaultProtectionTakeoverMatrixTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10982HealthyAssistanceAuthorityMatrixTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer.OperatorComputerM107SessionWorkspaceTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.ControlRoomPresentationContractTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.PlantControlAuthorityIntegrationTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.Views.OperatorExperienceM1091HmiShellTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.Views.ControlRoomComputerControlXamlContractTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10973MissionPerformanceWorkspaceUiTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceDrillDownUiTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ViewModels.M10982Hotfix1Rev5ListRefreshStabilityTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.Views.OperatorComputerM10954ObservedResponseXamlTests" --parallel none
if errorlevel 1 exit /b 1
dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.Views.OperatorComputerM10953ContextInspectorXamlTests" --parallel none
if errorlevel 1 exit /b 1

> "artifacts\m1098-integrated-hmi-closure\01-m10985-integrated-hmi-closure-preflight.summary.txt" echo scope=M10.9.8.5 Manual Integrated HMI Acceptance and M10.9.8 Closure over M10.9.8.4 Hotfix 1 VALIDATED; manual/docs closure-only; production runtime, compiled/test surface and Simulation physics unchanged;
>> "artifacts\m1098-integrated-hmi-closure\01-m10985-integrated-hmi-closure-preflight.summary.txt" echo manual-contract=m10985-manual-integrated-hmi-acceptance-v1; manual-routes=12; m10981-matrix-prerequisite=True; m10982-healthy-matrix-prerequisite=True; m10983-degraded-matrix-prerequisite=True; m10984-replay-checkpoint-prerequisite=True;
>> "artifacts\m1098-integrated-hmi-closure\01-m10985-integrated-hmi-closure-preflight.summary.txt" echo hmi-shell-owner-rerun=True; computer-f1-f8-owner-rerun=True; mission-workspace-owner-rerun=True; mission-drilldown-owner-rerun=True; interactive-list-stability-owner-rerun=True; session-checkpoint-owner-rerun=True; observed-response-owner-rerun=True; command-context-owner-rerun=True; authority-owner-rerun=True;
>> "artifacts\m1098-integrated-hmi-closure\01-m10985-integrated-hmi-closure-preflight.summary.txt" echo production-runtime-changed=False; compiled-surface-changed=False; test-surface-changed=False; m10985-integrated-hmi-closure-preflight-passes=True; manual-integrated-hmi-acceptance-required=True; m1098-closure-pending-manual=True; m10-closure-pending-final-pre-m11=True; next-step=manual M10.9.8.5 acceptance then M10 final cumulative and approximately-one-hour long validation;

echo.
echo === M10.9.8.5 artifact summary ===
type "artifacts\m1098-integrated-hmi-closure\01-m10985-integrated-hmi-closure-preflight.summary.txt"
echo.
echo M10.9.8.5 automated integrated-HMI closure preflight completed.
echo Promotion requires docs\M10_9_8_5_MANUAL_INTEGRATED_HMI_ACCEPTANCE_CHECKLIST.md and explicit manual acceptance.
echo After M10.9.8 closure, M10 remains OPEN until the final cumulative and long pre-M11 gates pass.
exit /b 0
