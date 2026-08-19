@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.27 off-design robustness / qualification-envelope gate...
echo.
echo H.27 does NOT rerun the 4.5-hour H.24 qualification.
echo H.26 fail-closed evidence is frozen and public production construction remains unchanged.
echo Safe rollback/fallback or canonical protection action may define an envelope boundary; unsafe corrected ownership may not.
echo.

echo Running H.27 descriptor and frozen-H.26 evidence contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeOffDesignQualificationEnvelopeAuditTests.FrozenH26Evidence_RetainsValidatedAtomicFailClosedFallbackContract" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running unchanged H.20/H.22 authority and H.26 public-constructor isolation contracts...
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

set "REPORT_DIR=%CD%\artifacts\h27-four-node-off-design-qualification-envelope"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.27 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeOffDesignQualificationEnvelopeAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-off-design-qualification-envelope.summary.txt"
    "02-off-design-step-telemetry.csv"
    "03-off-design-qualification-envelope.csv"
    "04-off-design-qualification-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.27 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-off-design-qualification-envelope.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.27 off-design robustness / qualification-envelope gate completed.
exit /b 0
