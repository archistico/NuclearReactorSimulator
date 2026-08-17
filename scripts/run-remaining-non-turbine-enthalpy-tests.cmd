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

echo Running M10.9.4.1-G.3 focused remaining non-turbine enthalpy migration tests...

dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Domain.Tests.Physics.Fluids.RemainingNonTurbineEnergyTransportDefinitionTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.PumpFlowSolverTests" ^
    "NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.PrimaryCircuit.Boundaries.PrimaryCircuitBoundarySolverTests" ^
    "NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.PrimaryCircuit.SteamDrums.SteamDrumSeparationSolverTests" ^
    "NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.Condenser.CondenserSystemSolverTests" ^
    "NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.MainSteam.MainSteamReliefBoundarySolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PassiveHydraulicEnthalpyMigrationAuditTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.RemainingNonTurbineEnthalpyMigrationAuditTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.MainSteamReliefImplementationTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.TurbineBypassImplementationTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\g3-remaining-non-turbine-enthalpy"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" (
    echo ERROR: unable to clear "%REPORT_DIR%".
    exit /b 1
)

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=RemainingNonTurbineEnthalpyMigrationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-current-v2-remaining-non-turbine-enthalpy.csv"
    "01-current-v2-remaining-non-turbine-enthalpy.summary.txt"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected G.3 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
echo ================================================================================
echo M10.9.4.1-G.3 REMAINING NON-TURBINE ENTHALPY MIGRATION SUMMARY
echo ================================================================================
type "%REPORT_DIR%\01-current-v2-remaining-non-turbine-enthalpy.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-G.3 focused remaining non-turbine enthalpy migration tests passed.
exit /b 0
