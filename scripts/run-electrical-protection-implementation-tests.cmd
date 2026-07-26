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

echo Running M10.9.4.1-E.3.2 focused electrical-protection tests...

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Control.Protection.ProtectionSystemSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.ElectricalProtectionImplementationTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Synchronization.GridSynchronizationSustainedInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Training.DesktopSustainedGenerationInitialConditionFactoryTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\e3-protection-implementation"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" (
    echo ERROR: unable to clear "%REPORT_DIR%".
    exit /b 1
)

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=ElectricalProtectionImplementationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-normal-five-zero-five.csv"
    "01-normal-five-zero-five.summary.txt"
    "02-turbine-trip-reverse-power-trip.csv"
    "02-turbine-trip-reverse-power-trip.summary.txt"
    "03-breaker-open-coastdown-supervision.csv"
    "03-breaker-open-coastdown-supervision.summary.txt"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected E.3.2 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
echo ================================================================================
echo M10.9.4.1-E.3.2 ELECTRICAL PROTECTION IMPLEMENTATION SUMMARY
echo ================================================================================
for %%F in ("%REPORT_DIR%\*.summary.txt") do (
    type "%%~fF"
)
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-E.3.2 focused electrical-protection tests passed.
exit /b 0
