@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-I.3 Phase-I reference trajectory / conservation-inventory baseline gate...
echo.
echo I.2 must already be validated. This gate runs one exact-v2 healthy 300-second reference journey.
echo It records one-second samples, consolidated conservation/inventory evidence, final-window slopes and v1 tolerance budgets.
echo It does not rerun H.24/H.28 and does not change runtime physics or numerical policy.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIReferenceTrajectoryConservationInventoryBaselineAuditTests.FrozenI2Evidence_ProvesAuditCiBaselineBeforeReferenceBaseline" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIReferenceTrajectoryConservationInventoryBaselineAuditTests.ReferenceTrajectoryContract_IsExactVersionedAndBaselineEstablishing" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\i3-phase-i-reference-trajectory-conservation-inventory-baseline"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

rem I.3 is a scheduled/manual long gate. The environment opt-in prevents this 300 s run
rem from executing inside ordinary dotnet test even if runner explicit-test policy changes.
set "NRS_I3_LONG_AUDIT=1"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=PhaseIReferenceTrajectoryConservationInventoryBaselineAudit" ^
    --parallel none
if errorlevel 1 (
    echo.
    echo I.3 Hotfix 2 focused diagnostic run completed with a red health gate. Generated evidence follows.
    if exist "%REPORT_DIR%\01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt" type "%REPORT_DIR%\01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"
    if exist "%REPORT_DIR%\06-generation-health-violations.csv" echo Generation-health diagnostics: "%REPORT_DIR%\06-generation-health-violations.csv"
    if exist "%REPORT_DIR%\07-shaft-drop-episodes.csv" echo Shaft-drop episodes: "%REPORT_DIR%\07-shaft-drop-episodes.csv"
    exit /b 1
)

for %%F in (
    "00-progress.txt"
    "01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"
    "02-reference-trajectory-contract.csv"
    "03-reference-trajectory-samples.csv"
    "04-conservation-inventory-final-window-slopes.csv"
    "05-versioned-tolerance-budgets.csv"
    "06-generation-health-violations.csv"
    "07-shaft-drop-episodes.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected I.3 artifact missing: %%F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-I.3 Phase-I reference trajectory / conservation-inventory baseline gate completed.
exit /b 0
