@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.28.1-C H.9 Jacobian/probe allocation and hot-path optimization gate...
echo.
echo H.28.1-C preserves H.9 finite-difference Newton mathematics, work counts and H.20/H.22 ownership.
echo The optimization removes transient full-PlantState materialization, repeated hydraulic topology lookup and duplicate evaluation canonical copies from the Jacobian probe path.
echo H.28 remains failed and H.29 remains blocked until the full H.28 performance gate is rerun successfully.
echo.

echo Running descriptor and frozen H.27/H.28/H.28.1-A provenance contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeH9JacobianHotPathOptimizationAuditTests.FrozenValidatedH281AEvidence_AnchorsOptimizationToMeasuredCostCenters" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running unchanged hydraulic and thermodynamic numerical unit contracts...
dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.SemiImplicitHydraulicPrototypeSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.SimplifiedWaterSteamThermodynamicModelTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.JacobianHydraulicCorrectorSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h28-1c-h9-jacobian-hot-path-optimization"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.28.1-C heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeH9JacobianHotPathOptimizationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-h9-jacobian-probe-hot-path-optimization.summary.txt"
    "02-h9-hot-path-optimization-steps.csv"
    "03-h9-hot-path-cost-centers.csv"
    "04-h9-hot-path-optimization-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.28.1-C audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-h9-jacobian-probe-hot-path-optimization.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.28.1-C H.9 Jacobian/probe hot-path optimization gate completed.
exit /b 0
