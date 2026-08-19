@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-I.2 Phase-I audit consolidation / CI baseline gate...
echo.
echo I.1 Hotfix 1 must already be validated. This gate does not execute historical H.5/H.21 numerical modes.
echo It freezes I.1 evidence, separates ordinary/current/scheduled/historical audit tiers and validates CI entry points.
echo No numerical mode is deleted by I.2.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIAuditConsolidationCiBaselineAuditTests.FrozenI1Evidence_ProvesCompatibilityBaselineBeforeAuditConsolidation" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIAuditConsolidationCiBaselineAuditTests.AuditTierManifest_SeparatesCurrentCiFromHistoricalFrozenProvenance" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIAuditConsolidationCiBaselineAuditTests.CiWorkflowContract_UsesGlobalJsonAndKeepsLongGatesOutOfOrdinaryCi" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\i2-phase-i-audit-consolidation-ci-baseline"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=PhaseIAuditConsolidationCiBaselineAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-phase-i-audit-consolidation-ci-baseline.summary.txt"
    "02-phase-i-audit-tier-manifest.csv"
    "03-legacy-mode-retirement-readiness.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected I.2 artifact missing: %%F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-phase-i-audit-consolidation-ci-baseline.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-I.2 Phase-I audit consolidation / CI baseline gate completed.
exit /b 0
