@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-I.1 profile compatibility / legacy retirement inventory gate...
echo.
echo Phase H must already be closed as OPT-IN ONLY. This gate does not retune or step long-horizon numerical qualification.
echo It inventories exact-version initial-condition compatibility and separates supported production modes from audit-only historical modes.
echo No exact-version profile is deleted by I.1.
echo.

echo Running descriptor, frozen H.30 evidence and exact-version inventory contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.ProfileCompatibilityLegacyRetirementInventoryAuditTests.FrozenH30Evidence_ProvesPhaseHClosedAndPhaseIUnblocked" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.ProfileCompatibilityLegacyRetirementInventoryAuditTests.ExactVersionInventory_EnumeratesSupportedCompatibilityWithoutDeletingReplayIdentities" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\i1-profile-compatibility-legacy-retirement-inventory"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=ProfileCompatibilityLegacyRetirementInventoryAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-phase-i-profile-compatibility-legacy-retirement-inventory.summary.txt"
    "02-profile-compatibility-matrix.csv"
    "03-numerical-mode-retirement-inventory.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected I.1 artifact missing: %%F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-phase-i-profile-compatibility-legacy-retirement-inventory.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-I.1 profile compatibility / legacy retirement inventory gate completed.
exit /b 0
