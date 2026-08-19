@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.28.1-A corrected-path performance attribution gate...
echo.
echo H.28.1-A is diagnostic only: no performance ceilings are relaxed and no numerical optimization is applied.
echo The package starts from validated H.27 Hotfix 1 and freezes the failed H.28 evidence as diagnostic provenance.
echo H.24 is not rerun. Standard current-v2 remains ExplicitCommittedState.
echo.

echo Running H.28.1-A descriptor, frozen validated-H.27 baseline evidence and failed-H.28 diagnostic evidence contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodePerformanceAttributionAuditTests.FrozenValidatedH27Evidence_AnchorsPerformanceAttributionToAuthoritativeBaseline" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodePerformanceAttributionAuditTests.FrozenFailedH28Evidence_RetainsObservedUnboundedRegressionWithoutPromotingItToBaseline" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running unchanged H.9 numerical and H.26 deterministic telemetry-equality sentinels...
dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.JacobianHydraulicCorrectorSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Simulation.Tests.Plant.FourNodeIntegratedRollbackFailClosedStressAuditTests.PublicOrchestrator_H26DecisionTransformIsAbsentAndIdentityAuditHookIsTransparent" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h28-1a-four-node-performance-attribution"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.28.1-A heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodePerformanceAttributionAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-performance-attribution.summary.txt"
    "02-performance-attribution-steps.csv"
    "03-performance-attribution-cost-centers.csv"
    "04-performance-attribution-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.28.1-A audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-performance-attribution.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.28.1-A corrected-path performance attribution gate completed.
exit /b 0
