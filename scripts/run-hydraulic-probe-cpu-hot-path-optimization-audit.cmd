@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.28.1-D hydraulic probe CPU hot-path optimization gate...
echo.
echo H.28.1-D preserves H.28.1-B predictor reuse and the unchanged 32-probe finite-difference Newton contract.
echo It removes only exact duplicate probe thermodynamic work and reuses the immutable 513-point saturated coarse-scan properties.
echo H.28 remains failed and H.29 remains blocked until the full H.28 performance gate is rerun successfully.
echo.

echo Running descriptor and frozen H.27/H.28/H.28.1-A/H.28.1-C/H.28.1-B provenance contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeHydraulicProbeCpuHotPathOptimizationAuditTests" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running unchanged thermodynamic, hydraulic and H.9 numerical contracts...
dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.SimplifiedWaterSteamThermodynamicModelTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.SemiImplicitHydraulicPrototypeSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.JacobianHydraulicCorrectorSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h28-1d-hydraulic-probe-cpu-hot-path-optimization"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.28.1-D heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeHydraulicProbeCpuHotPathOptimizationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-hydraulic-probe-cpu-hot-path-optimization.summary.txt"
    "02-hydraulic-probe-cpu-hot-path-optimization-steps.csv"
    "03-hydraulic-probe-cpu-hot-path-optimization-cost-centers.csv"
    "04-hydraulic-probe-cpu-hot-path-optimization-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.28.1-D audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-hydraulic-probe-cpu-hot-path-optimization.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.28.1-D hydraulic probe CPU hot-path optimization gate completed.
exit /b 0
