@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.30 Phase H closure / production qualification decision gate...
echo.
echo This is an evidence-only closure gate: H.24 and H.28 are NOT rerun.
echo Validated H.19-H.29 evidence is fingerprint-checked and the end-of-Phase-H policy is derived fail-closed.
echo Candidate closure decision: OPT-IN ONLY because H.28 remains bounded-but-costly.
echo v2 ExplicitCommittedState must remain authoritative default/rollback/reference; v3 corrected remains qualified opt-in.
echo.

echo Running descriptor, frozen-evidence and closure-policy contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodePhaseHClosureDecisionAuditTests.FrozenPhaseHEvidence_RetainsValidatedH19ThroughH29Chain" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodePhaseHClosureDecisionAuditTests.ClosurePolicyContract_PreservesExactV2RollbackAndExactV3IdentityAfterPolicyRequalification" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h30-phase-h-closure-production-qualification-decision"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodePhaseHClosureDecisionAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-phase-h-closure-production-qualification-decision.summary.txt"
    "02-phase-h-closure-decision-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.30 artifact missing: %%F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-phase-h-closure-production-qualification-decision.summary.txt"
echo Detailed CSV file: "%REPORT_DIR%\02-phase-h-closure-decision-metrics.csv"
echo M10.9.4.1-H.30 Phase H closure / production qualification decision gate completed.
exit /b 0
