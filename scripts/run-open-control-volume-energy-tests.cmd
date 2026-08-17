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

echo Running M10.9.4.1-G.1 focused open-control-volume energy-convention tests...

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.OpenControlVolumeEnergyTransportSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.OpenControlVolumeEnergyTransportAuditTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\g1-energy-transport-convention"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" (
    echo ERROR: unable to clear "%REPORT_DIR%".
    exit /b 1
)

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=OpenControlVolumeEnergyTransportAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-current-v2-representative-open-control-volume-gap.csv"
    "01-current-v2-representative-open-control-volume-gap.summary.txt"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected G.1 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
echo ================================================================================
echo M10.9.4.1-G.1 OPEN-CONTROL-VOLUME ENERGY CONVENTION SUMMARY
echo ================================================================================
type "%REPORT_DIR%\01-current-v2-representative-open-control-volume-gap.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-G.1 focused open-control-volume energy-convention tests passed.
exit /b 0
