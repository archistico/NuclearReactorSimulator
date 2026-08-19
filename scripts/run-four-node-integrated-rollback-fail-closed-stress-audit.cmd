@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.26 integrated rollback / fail-closed stress gate...
echo.
echo H.26 does NOT rerun the 4.5-hour H.24 qualification.
echo User-validated H.25 evidence is frozen and fingerprint-checked first.
echo The H.26 authority-decision transform is internal test infrastructure only; public production construction remains unchanged.
echo.

echo Running H.26 descriptor and frozen-H.25 evidence contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeIntegratedRollbackEvidenceContractTests.FrozenH25Evidence_RetainsValidatedProtectionTransientMatrixAndDefaultExplicitIsolation" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running unchanged H.20 typed-reason semantics and H.26 public-constructor isolation contracts...
dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.FourNodeBranchContinuityShadowActivationSupervisorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.FourNodeBranchContinuityCorrectedCommitSeamTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Simulation.Tests.Plant.FourNodeIntegratedRollbackFailClosedStressAuditTests.PublicOrchestrator_H26DecisionTransformIsAbsentAndIdentityAuditHookIsTransparent" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h26-four-node-integrated-rollback-fail-closed-stress"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.26 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.
echo NOTE: H.26 is a short fail-closed integration stress gate. H.24 remains frozen evidence unless the committed numerical runtime changes.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeIntegratedRollbackFailClosedStressAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-integrated-rollback-fail-closed-stress.summary.txt"
    "02-integrated-rollback-challenges.csv"
    "03-integrated-rollback-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.26 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-integrated-rollback-fail-closed-stress.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.26 integrated rollback / fail-closed stress gate completed.
exit /b 0
