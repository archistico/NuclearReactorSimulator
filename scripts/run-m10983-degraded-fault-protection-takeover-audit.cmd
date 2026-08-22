@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "SIMULATION_PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"

echo Running M10.9.8.3 Degraded Measurement / Fault / Protection / Takeover audit...
echo.
echo Scope: execute the eleven frozen degraded/fault/protection/takeover cases using existing M4.5/M5/M8/M10.9.6 owners.
echo Validation-only compositions may bind existing exact-v4/fault seams but add no production scenario, fault type or Simulation physics.
echo Replay/checkpoint equivalence remains M10.9.8.4 ownership.
echo.

if exist "artifacts\m1098-degraded-fault-protection-takeover" rd /s /q "artifacts\m1098-degraded-fault-protection-takeover"

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for M10.9.8.3 matrix validation.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10981-integrated-validation-matrix.ps1" -RepositoryRoot "%CD%" -HistoricalReuse
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10982-integrated-validation-matrix-v2.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10983-degraded-fault-protection-takeover-matrix.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.M10983DegradedFaultProtectionTakeoverMatrixTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.PlantControlAuthorityIntegrationTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Faults.InstrumentationControlFaultTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Faults.HydraulicComponentFaultTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Faults.FaultInjectionFrameworkTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.SafetyResponse.SafetyResponseScenarioPackTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer.OperatorComputerM10954ObservedResponseEvidenceTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.M10961ChallengeLifecycleContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Demand.M10962ExternalEnergyDemandProfileTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Scoring.M10963ChallengeScoringContractTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%SIMULATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Control.Protection.ProtectionSystemSolverTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%SIMULATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests" --parallel none
if errorlevel 1 exit /b 1

if not exist "artifacts\m1098-degraded-fault-protection-takeover\01-m10983-degraded-fault-protection-takeover.summary.txt" (
  echo ERROR: expected M10.9.8.3 integration summary artifact was not written.
  exit /b 1
)

> "artifacts\m1098-degraded-fault-protection-takeover\02-m10983-owner-rerun.summary.txt" echo blocked-permissive-owner-rerun=True; protection-owner-rerun=True; authority-owner-rerun=True; hydraulic-fault-owner-rerun=True; instrumentation-fault-owner-rerun=True; fault-framework-owner-rerun=True; observed-response-owner-rerun=True; challenge-lifecycle-demand-scoring-owner-rerun=True; all-eleven-m10983-cases-covered=True; m10983-degraded-fault-protection-takeover-passes=True;

echo.
echo === M10.9.8.3 artifact summary ===
type "artifacts\m1098-degraded-fault-protection-takeover\01-m10983-degraded-fault-protection-takeover.summary.txt"
echo.
type "artifacts\m1098-degraded-fault-protection-takeover\02-m10983-owner-rerun.summary.txt"
echo.
echo M10.9.8.3 automated degraded measurement / fault / protection / takeover audit completed.
echo Next: M10.9.8.4 replay/checkpoint/same-seed integrity.
exit /b 0
