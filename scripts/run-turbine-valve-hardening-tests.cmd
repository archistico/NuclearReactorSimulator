@echo off
setlocal EnableExtensions

cd /d "%~dp0\.."

if not exist "NuclearReactorSimulator.sln" (
    echo ERROR: repository root not found.
    exit /b 1
)

echo Running M10.9.4.1-D.4.1 turbine-valve hardening tests...

dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.MainSteam.MainSteamNetworkDefinitionTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.ControlRoom.TurbineValveOperatorControlTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.PreStartup.ColdShutdownInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Recording.TurbineValveReplayCheckpointTests" ^
    --parallel none
if errorlevel 1 exit /b 1

echo M10.9.4.1-D.4.1 focused tests passed.
exit /b 0
