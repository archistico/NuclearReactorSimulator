@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-failure-diagnostic5"
set "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC5=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"

echo M10 Final replacement-long failure Diagnostic 5
echo Diagnostic 4 PASS: fixed-time smaller/slower ramps and short energy-support lead did not establish stable 10 MWe.
echo Diagnostic 4's nominal 66 MWth pre-power target was not actually reached before the load step.
echo This gate changes no production source, workload, authority, generator-load semantics, protection, exact-v9 or mission pack.
echo It gates each test-only load increment on measured thermal readiness and post-increment stabilization.
echo.
echo [1/2] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [2/2] Focused explicit readiness-gated staged-load diagnostic...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongFailureDiagnostic5Tests.ExactV9_ReadinessGatedStagedLoadAndAttainableCapacityCensus" --parallel none
if errorlevel 1 goto :fail

if not exist "%REPORT_DIR%\180-readiness-gated-probe-summary.csv" goto :missing
if not exist "%REPORT_DIR%\181-readiness-gated-stage-events.csv" goto :missing
if not exist "%REPORT_DIR%\182-readiness-gated-trajectories.csv" goto :missing
if not exist "%REPORT_DIR%\183-readiness-gated-decision-summary.txt" goto :missing

echo.
echo M10 Final replacement-long failure Diagnostic 5 completed.
echo Return the full "%REPORT_DIR%" folder before changing the replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
exit /b 0

:missing
echo.
echo M10 Final replacement-long failure Diagnostic 5 FAILED: expected diagnostic artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final replacement-long failure Diagnostic 5 FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
