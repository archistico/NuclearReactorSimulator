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

set "REPORT_DIR=%CD%\artifacts\e3-protection-trajectories"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" (
    echo ERROR: unable to clear "%REPORT_DIR%".
    exit /b 1
)

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- --explicit only --filter-trait "Category=ElectricalProtectionTrajectoryAudit" --parallel none
set "TEST_EXIT=%ERRORLEVEL%"
if not "%TEST_EXIT%"=="0" exit /b %TEST_EXIT%

if not exist "%REPORT_DIR%" (
    echo ERROR: the audit passed but no trajectory report directory was produced.
    exit /b 2
)

for %%F in (
    "01-normal-generation-load-step.csv"
    "01-normal-generation-load-step.summary.txt"
    "02-turbine-trip-reverse-power.csv"
    "02-turbine-trip-reverse-power.summary.txt"
    "03-disconnected-underfrequency-coastdown.csv"
    "03-disconnected-underfrequency-coastdown.summary.txt"
    "04-breaker-closed-phase-offset-sweep.summary.txt"
    "04-phase-offset-minus135.csv"
    "04-phase-offset-minus90.csv"
    "04-phase-offset-minus45.csv"
    "04-phase-offset-minus15.csv"
    "04-phase-offset-plus15.csv"
    "04-phase-offset-plus45.csv"
    "04-phase-offset-plus90.csv"
    "04-phase-offset-plus135.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected audit artifact missing: %%~F
        exit /b 3
    )
)

echo.
echo ================================================================================
echo M10.9.4.1-E.3.1 SIGNED ELECTRICAL PROTECTION TRAJECTORY SUMMARY
echo ================================================================================
for %%F in ("%REPORT_DIR%\*.summary.txt") do (
    type "%%~fF"
)
echo Detailed CSV files: "%REPORT_DIR%"
exit /b 0
