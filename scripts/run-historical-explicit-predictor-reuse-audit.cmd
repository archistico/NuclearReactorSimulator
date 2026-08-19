@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.28.1-B historical explicit predictor-reuse gate...
echo.
echo H.28.1-B reuses the historical explicit fluid-node predictor only where historical applied balances exactly match canonical H.4 balances.
echo Mismatched nodes are reintegrated through unchanged H.4 arithmetic; the committed hydraulic evaluation is reused and predictor-end P060/F040 evaluation remains unchanged.
echo H.28 remains failed and H.29 remains blocked until the full H.28 performance gate is rerun successfully.
echo.

echo Running descriptor and frozen H.27/H.28/H.28.1-A/H.28.1-C provenance contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeHistoricalExplicitPredictorReuseAuditTests" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running predictor-equivalence and unchanged H.9 numerical contracts...
dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Simulation.Tests.Plant.HybridSemiImplicitHydraulicGateSolverTests.HistoricalExplicitPredictorReuse_IsExactlyEquivalentToLegacyPredictorEvaluation" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Simulation.Tests.Plant.HybridSemiImplicitHydraulicGateSolverTests.HistoricalExplicitPredictorReuse_ReintegratesBalanceMismatchWithoutChangingPredictor" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.JacobianHydraulicCorrectorSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h28-1b-historical-explicit-predictor-reuse"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.28.1-B heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeHistoricalExplicitPredictorReuseAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-historical-explicit-predictor-reuse.summary.txt"
    "02-historical-explicit-predictor-reuse-steps.csv"
    "03-historical-explicit-predictor-reuse-cost-centers.csv"
    "04-historical-explicit-predictor-reuse-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.28.1-B audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-historical-explicit-predictor-reuse.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.28.1-B historical explicit predictor-reuse gate completed.
exit /b 0
