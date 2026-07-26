@echo off
setlocal EnableExtensions

cd /d "%~dp0\.."

if not exist "NuclearReactorSimulator.sln" (
    echo ERROR: repository root not found.
    exit /b 1
)

echo Running M10.9.4.1-E.2 focused generator/grid tests...

dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Domain.Tests.Physics.Electrical.ElectricalQuantityTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Training.DesktopSustainedGenerationInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization.GridSynchronizationSustainedInitialConditionFactoryTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=ReferencePlantScaleAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

echo M10.9.4.1-E.2 focused generator/grid tests passed.
exit /b 0
