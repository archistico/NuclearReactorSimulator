@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.5 Hotfix 2 production rollback and extended shadow-qualification tests...

dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Domain.Tests.Plant.HydraulicNumericalCouplingDefinitionTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Simulation.Tests.Plant.SemiImplicitHydraulicPrototypeSolverTests" ^
    "NuclearReactorSimulator.Simulation.Tests.Plant.HybridSemiImplicitHydraulicGateSolverTests" ^
    "NuclearReactorSimulator.Simulation.Tests.Plant.HybridPlantNetworkProductionIntegrationTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Training.DesktopSustainedGenerationInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization.GridSynchronizationSustainedInitialConditionFactoryTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h5-hybrid-production-integration"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=HybridProductionIntegrationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-current-v2-shadow-qualification-trajectory.csv"
    "01-current-v2-shadow-qualification.summary.txt"
    "02-current-v2-shadow-correction-events.csv"
    "03-current-v2-shadow-final-candidate.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.5 Hotfix 2 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-current-v2-shadow-qualification.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.5 Hotfix 2 production rollback and extended shadow-qualification tests passed.
exit /b 0
