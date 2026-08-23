@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-closure-plan1-p1"
set "NRS_M10_FINAL_REPLACEMENT_LONG_P1=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P1
echo ASYMPTOTIC FIRST-STAGE QUALIFICATION
echo ============================================================
echo P0 Hotfix 2 is VALIDATED.
echo P1 is the last planned purely exploratory dynamics gate.
echo No production runtime, workload, authority, generator-load,
echo protection, exact-v9 or mission-pack change is authorized.
echo.
echo [1/2] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [2/2] Focused explicit P1 asymptotic qualification...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongClosurePlan1P1AsymptoticFirstStageQualificationTests.ExactV9_AsymptoticFirstStageQualification_ClassifiesPlannedDecisionBranch" --parallel none
if errorlevel 1 goto :fail

if not exist "%REPORT_DIR%\01-reference-noise-calibration.csv" goto :missing
if not exist "%REPORT_DIR%\02-asymptotic-probe-summary.csv" goto :missing
if not exist "%REPORT_DIR%\03-asymptotic-events.csv" goto :missing
if not exist "%REPORT_DIR%\04-asymptotic-trajectories.csv" goto :missing
if not exist "%REPORT_DIR%\05-p1-decision-summary.txt" goto :missing

echo.
echo M10 Final Replacement-Long Closure Plan 1 P1 completed.
echo Return the full "%REPORT_DIR%" folder before P2 branch selection, changing the replacement workload, changing runtime semantics, or freezing a second replacement-long baseline.
exit /b 0

:missing
echo.
echo M10 Final Replacement-Long Closure Plan 1 P1 FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final Replacement-Long Closure Plan 1 P1 FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
