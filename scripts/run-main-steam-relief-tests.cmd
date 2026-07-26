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

echo Running M10.9.4.1-F.2 focused main-steam relief tests...

dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.MainSteam.MainSteamReliefBoundaryDefinitionTests" ^
    "NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.MainSteam.MainSteamNetworkDefinitionTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.MainSteam.MainSteamReliefBoundarySolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.MainSteamReliefImplementationTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization.GridSynchronizationSustainedInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Training.DesktopSustainedGenerationInitialConditionFactoryTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\f2-main-steam-relief"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" (
    echo ERROR: unable to clear "%REPORT_DIR%".
    exit /b 1
)

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=MainSteamReliefImplementationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-current-v2-header-relief-pressure-sweep.csv"
    "01-current-v2-header-relief-pressure-sweep.summary.txt"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected F.2 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
echo ================================================================================
echo M10.9.4.1-F.2 MAIN-STEAM HEADER RELIEF SUMMARY
echo ================================================================================
for %%F in ("%REPORT_DIR%\*.summary.txt") do (
    type "%%~fF"
)
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-F.2 focused main-steam relief tests passed.
exit /b 0
