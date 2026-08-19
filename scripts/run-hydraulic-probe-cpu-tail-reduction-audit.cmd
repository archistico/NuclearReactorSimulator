@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.28.1-E exact hydraulic probe CPU-tail reduction gate...
echo.
echo H.28.1-E starts from validated H.28.1-D and frozen failed H.28 Requalification 1 p95 evidence.
echo It preserves 32 finite-difference probes, 35 logical hydraulic evaluations, H.9 Newton mathematics, H.20/H.22 ownership and P060/F040.
echo H.28 remains failed and H.29 remains blocked until the unchanged H.28 performance gate is green.
echo.

echo Running descriptor and frozen H.28.1-D / H.28 Requalification 1 provenance contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeHydraulicProbeCpuTailReductionAuditTests" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running exact-equivalence thermodynamic/hydraulic contracts and unchanged H.9 numerical tests...
dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.SimplifiedWaterSteamThermodynamicModelTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.ThermodynamicBranchContinuityModelTests" ^
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

set "REPORT_DIR=%CD%\artifacts\h28-1e-hydraulic-probe-cpu-tail-reduction"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.28.1-E heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeHydraulicProbeCpuTailReductionAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-hydraulic-probe-cpu-tail-reduction.summary.txt"
    "02-hydraulic-probe-cpu-tail-reduction-steps.csv"
    "03-hydraulic-probe-cpu-tail-reduction-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.28.1-E audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-hydraulic-probe-cpu-tail-reduction.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.28.1-E exact hydraulic probe CPU-tail reduction gate completed.
exit /b 0
