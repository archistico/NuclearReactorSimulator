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

echo Running M10.9.4.1-H.3 isolated semi-implicit hydraulic prototype tests...

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.SemiImplicitHydraulicPrototypeSolverTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class ^
    "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Training.DesktopSustainedGenerationInitialConditionFactoryTests" ^
    "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.SemiImplicitHydraulicPrototypeAuditTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h3-semi-implicit-hydraulic-prototype"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" (
    echo ERROR: unable to clear "%REPORT_DIR%".
    exit /b 1
)

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=SemiImplicitHydraulicPrototypeAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-current-v2-semi-implicit-hydraulic-prototype.csv"
    "01-current-v2-semi-implicit-hydraulic-prototype.summary.txt"
    "02-current-v2-semi-implicit-final-state.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.3 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
echo ================================================================================
echo M10.9.4.1-H.3 SEMI-IMPLICIT HYDRAULIC PROTOTYPE SUMMARY
echo ================================================================================
type "%REPORT_DIR%\01-current-v2-semi-implicit-hydraulic-prototype.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.3 isolated semi-implicit hydraulic prototype tests passed.
exit /b 0
