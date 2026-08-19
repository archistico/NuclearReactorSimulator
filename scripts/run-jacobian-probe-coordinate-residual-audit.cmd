@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.28.1-F Jacobian probe coordinate-residual specialization gate...
echo.
echo H.28.1-F is rebased on validated H.28.1-D, freezes failed H.28.1-E evidence, and targets the unchanged H.28 p95 ceiling.
echo It preserves 32 finite-difference probes, 35 logical hydraulic evaluations, H.9 Newton mathematics, H.20/H.22 ownership and P060/F040.
echo H.28 remains failed and H.29 remains blocked until the unchanged H.28 performance gate is green.
echo.

echo Running descriptor and frozen H.28.1-D / H.28 Requalification 1 / H.28.1-E provenance contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeJacobianProbeCoordinateResidualAuditTests" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running exact-equivalence thermodynamic/hydraulic/Jacobian contracts...
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

set "REPORT_DIR=%CD%\artifacts\h28-1f-jacobian-probe-coordinate-residual"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.28.1-F heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeJacobianProbeCoordinateResidualAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-jacobian-probe-coordinate-residual.summary.txt"
    "02-jacobian-probe-coordinate-residual-steps.csv"
    "03-jacobian-probe-coordinate-residual-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.28.1-F audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-jacobian-probe-coordinate-residual.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.28.1-F Jacobian probe coordinate-residual specialization gate completed.
exit /b 0
