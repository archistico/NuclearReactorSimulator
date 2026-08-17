@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.17 long-horizon and cross-profile branch-continuity shadow qualification...

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.ThermodynamicBranchContinuityModelTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h17-long-horizon-cross-profile-branch-continuity"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.17 Hotfix 4 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo If the long gate appears quiet, inspect that file from another console instead of waiting blindly.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=LongHorizonCrossProfileBranchContinuityQualificationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-current-v2-long-horizon-cross-profile-branch-continuity.summary.txt"
    "02-profile-qualification-summary.csv"
    "03-triggered-event-cross-profile-results.csv"
    "03a-trigger-episode-stratification.csv"
    "04-committed-target-selection-observations.csv"
    "05-triggered-all-node-inverse-branch-scan.csv"
    "06-hysteresis-release-challenges.csv"
    "07-long-horizon-cross-profile-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.17 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-current-v2-long-horizon-cross-profile-branch-continuity.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.17 long-horizon and cross-profile branch-continuity shadow qualification audit passed.
exit /b 0
