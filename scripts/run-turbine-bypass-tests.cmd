@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 (
    echo ERROR: unable to enter repository root "%ROOT%".
    exit /b 1
)

if not exist "NuclearReactorSimulator.sln" (
    echo ERROR: repository root not found.
    exit /b 1
)

echo Running M10.9.4.1-F.3 focused turbine-bypass tests...

dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Condenser.TurbineBypassDefinitionTests" ^
    "NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Condenser.CondenserSystemDefinitionTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Condenser.CondenserSystemSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.TurbineBypassImplementationTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization.GridSynchronizationSustainedInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Training.DesktopSustainedGenerationInitialConditionFactoryTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\f3-turbine-bypass"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" (
    echo ERROR: unable to clear "%REPORT_DIR%".
    exit /b 1
)

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=TurbineBypassImplementationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-current-v2-turbine-bypass-source-pressure-sweep.csv"
    "01-current-v2-turbine-bypass-source-pressure-sweep.summary.txt"
    "02-current-v2-turbine-bypass-condenser-backpressure-sweep.csv"
    "02-current-v2-turbine-bypass-condenser-backpressure-sweep.summary.txt"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected F.3 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
echo ================================================================================
echo M10.9.4.1-F.3 TURBINE BYPASS SUMMARY
echo ================================================================================
for %%F in ("%REPORT_DIR%\*.summary.txt") do (
    type "%%~fF"
)
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-F.3 focused turbine-bypass tests passed.
exit /b 0
