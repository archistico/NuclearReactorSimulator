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

echo Running M10.9.4.1-G.4 focused turbine-expansion enthalpy migration tests...

dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Turbine.TurbineExpansionSystemDefinitionTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Turbine.TurbineExpansionSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Training.DesktopSustainedGenerationInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization.GridSynchronizationSustainedInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.TurbineExpansionEnthalpyMigrationAuditTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\g4-turbine-expansion-enthalpy"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" (
    echo ERROR: unable to clear "%REPORT_DIR%".
    exit /b 1
)

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=TurbineExpansionEnthalpyMigrationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-current-v2-turbine-expansion-enthalpy-and-shaft-work.csv"
    "01-current-v2-turbine-expansion-enthalpy-and-shaft-work.summary.txt"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected G.4 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
echo ================================================================================
echo M10.9.4.1-G.4 TURBINE EXPANSION ENTHALPY MIGRATION SUMMARY
echo ================================================================================
type "%REPORT_DIR%\01-current-v2-turbine-expansion-enthalpy-and-shaft-work.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-G.4 focused turbine-expansion enthalpy migration tests passed.
exit /b 0
