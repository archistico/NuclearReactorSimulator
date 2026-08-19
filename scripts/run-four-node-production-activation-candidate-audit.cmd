@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.29 production activation candidate gate...
echo.
echo H.23-H.28 and the green post-H.28 H.24 requalification are fingerprint-checked as frozen prerequisites.
echo The H.29 runtime candidate is exact initial-condition v3; v2 ExplicitCommittedState remains the authoritative default and kill/rollback reference.
echo H.30, not H.29, owns the final ACTIVATE / OPT-IN ONLY / REMAIN EXPLICIT decision.
echo.

echo Running descriptor, policy, telemetry and frozen-evidence contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Training.DesktopHydraulicProductionPolicyTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeProductionActivationTelemetryCounterTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeProductionActivationCandidateAuditTests.FrozenPrerequisites_RetainValidatedH23ThroughH28AndPostOptimizationH24Evidence" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h29-four-node-production-activation-candidate"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.29 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeProductionActivationCandidateAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-production-activation-candidate.summary.txt"
    "02-production-activation-candidate-step-telemetry.csv"
    "03-production-activation-candidate-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.29 artifact missing: %%F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-production-activation-candidate.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.29 production activation candidate gate completed.
exit /b 0
