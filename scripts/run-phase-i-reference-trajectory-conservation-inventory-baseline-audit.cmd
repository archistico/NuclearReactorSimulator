@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-I.3 authoritative production reference / conservation-inventory / tolerance-baseline gate...
echo.
echo H.30 RQ1 must already be validated with ACTIVATE.
echo This scheduled/manual gate runs the authoritative production selector for 300 simulated seconds.
echo Generation health and stop/control/admission flow direction are checked every 10 ms.
echo One-second samples establish conservation/inventory slopes and 19 versioned regression budgets.
echo No runtime physics or numerical mathematics are retuned by this gate.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIReferenceTrajectoryConservationInventoryBaselineAuditTests.H30Rq1ValidatedManifest_RecordsActivatedProductionPolicyWithoutBundlingAuditPayloads" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIReferenceTrajectoryConservationInventoryBaselineAuditTests.ProductionSelector_IsValidatedH30Rq1V3WithExactV2FailClosedRollback" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIReferenceTrajectoryConservationInventoryBaselineAuditTests.ProductionReferenceContract_IsAuthoritativeV3AndBudgetEstablishing" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\i3-phase-i-authoritative-reference-trajectory-baseline"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

set "NRS_I3_PRODUCTION_REFERENCE_AUDIT=1"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIReferenceTrajectoryConservationInventoryBaselineAuditTests.AuthoritativeProductionV3_ThreeHundredSeconds_EstablishesReferenceConservationInventoryAndToleranceBudgets" ^
    --parallel none
if errorlevel 1 (
    echo.
    echo I.3 authoritative production reference gate failed. Generated diagnostics follow if available.
    if exist "%REPORT_DIR%\01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt" type "%REPORT_DIR%\01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"
    if exist "%REPORT_DIR%\06-step-health-violations.csv" echo Step-health diagnostics: "%REPORT_DIR%\06-step-health-violations.csv"
    if exist "%REPORT_DIR%\07-targeted-reverse-flow-violations.csv" echo Targeted reverse-flow diagnostics: "%REPORT_DIR%\07-targeted-reverse-flow-violations.csv"
    exit /b 1
)

for %%F in (
    "00-progress.txt"
    "01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"
    "02-reference-trajectory-contract.csv"
    "03-reference-trajectory-samples.csv"
    "04-conservation-inventory-final-window-slopes.csv"
    "05-versioned-tolerance-budgets.csv"
    "06-step-health-violations.csv"
    "07-targeted-reverse-flow-violations.csv"
    "08-production-telemetry.csv"
    "09-determinism-control.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected I.3 artifact missing: %%F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-phase-i-reference-trajectory-conservation-inventory-baseline.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-I.3 authoritative production reference baseline gate completed.
exit /b 0
