@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "OUT=artifacts\h30-rq1-production-policy-rereview"

echo Running M10.9.4.1-H.30 Requalification 1 production-policy re-review...
echo.
echo This gate is evidence-only. It does not rerun H.24, H.28 or the I.3 long diagnostics.
echo It verifies frozen I.3 evidence plus the activated exact-v3 startup/default and exact-v2 kill/rollback contract.
echo.

echo Running frozen-evidence preflight...
dotnet test --project "%PROJECT%" -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeH30ProductionPolicyRereviewAuditTests.FrozenI3Evidence_ProvesExplicitDiscontinuityAndCorrectedThreeHundredSecondHealth" ^
  --parallel none
if errorlevel 1 exit /b 1

echo Running production-selector preflight...
dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeH30ProductionPolicyRereviewAuditTests.ProductionSelector_ActivatesExactV3ByDefaultAndKeepsExactV2FailClosedRollback" ^
  --parallel none
if errorlevel 1 exit /b 1

echo Running desktop-composition preflight...
dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeH30ProductionPolicyRereviewAuditTests.DesktopComposition_UsesProductionProgramInsteadOfHistoricalV2ProgramForFreshStartup" ^
  --parallel none
if errorlevel 1 exit /b 1

if exist "%OUT%" rd /s /q "%OUT%"

echo Running evidence-derived ACTIVATE decision...
dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeH30ProductionPolicyRereviewAuditTests.ValidatedEvidence_DerivesActivateWithoutNumericalRetuningAndWritesClosureRereview" ^
  --parallel none
if errorlevel 1 exit /b 1

if not exist "%OUT%\01-h30-rq1-production-policy-rereview.summary.txt" exit /b 1
if not exist "%OUT%\02-h30-rq1-production-policy-rereview-metrics.csv" exit /b 1

echo.
type "%OUT%\01-h30-rq1-production-policy-rereview.summary.txt"
echo.
echo M10.9.4.1-H.30 Requalification 1 production-policy re-review gate completed.
exit /b 0
