@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "APP_PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"

echo Running M10.9.8.4 Hotfix 1 Replay / Checkpoint / Same-Seed Integrity audit...
echo.
echo Scope: requalify representative healthy, degraded/recovered, protection-trip and manual-takeover states through
echo same exact-seed repeat, full replay, replay-backed checkpoint prefix/live continuation and challenge projection.
echo No production runtime, Simulation physics, archive schema, fingerprint algorithm, challenge/scoring/protection ownership or plant-command authority change.
echo.

if exist "artifacts\m1098-replay-checkpoint-same-seed-integrity" rd /s /q "artifacts\m1098-replay-checkpoint-same-seed-integrity"

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for M10.9.8.4 matrix validation.
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

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10984ReplayCheckpointSameSeedIntegrityTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10983DegradedFaultProtectionTakeoverMatrixTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10982HealthyAssistanceAuthorityMatrixTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Recording.ScenarioRecorderReplayTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Recording.ScenarioSessionArchiveReplayTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Recording.ScenarioAutomationReplayTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Replay.M10965ChallengeReplayCheckpointClosureTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.M10961ChallengeLifecycleContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceTimelineContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974FingerprintV1SchemaAnchorTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer.OperatorComputerM10954ObservedResponseEvidenceTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.AlarmTrendTimelinePresentationTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APP_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.App.Tests.ControlRoom.MissionPerformance.M10974MissionPerformanceArchiveRestoreTests" --parallel none
if errorlevel 1 exit /b 1

if not exist "artifacts\m1098-replay-checkpoint-same-seed-integrity\01-m10984-replay-checkpoint-same-seed-integrity.summary.txt" (
  echo ERROR: expected M10.9.8.4 integration summary artifact was not written.
  exit /b 1
)

> "artifacts\m1098-replay-checkpoint-same-seed-integrity\02-m10984-owner-rerun.summary.txt" echo recorder-replay-owner-rerun=True; session-archive-owner-rerun=True; automation-intent-replay-owner-rerun=True; challenge-replay-owner-rerun=True; challenge-same-seed-owner-rerun=True; mission-timeline-owner-rerun=True; mission-archive-restore-owner-rerun=True; command-observed-response-owner-rerun=True; alarm-timeline-owner-rerun=True; fingerprint-v1-owner-rerun=True; m10983-state-matrix-rerun=True; m10982-healthy-matrix-rerun=True; m10984-replay-checkpoint-same-seed-integrity-passes=True;

echo.
echo === M10.9.8.4 Hotfix 1 artifact summary ===
type "artifacts\m1098-replay-checkpoint-same-seed-integrity\01-m10984-replay-checkpoint-same-seed-integrity.summary.txt"
echo.
type "artifacts\m1098-replay-checkpoint-same-seed-integrity\02-m10984-owner-rerun.summary.txt"
echo.
echo M10.9.8.4 Hotfix 1 automated replay/checkpoint/same-seed integrity audit completed.
echo Next: M10.9.8.5 manual integrated HMI acceptance and M10.9.8 closure.
exit /b 0
