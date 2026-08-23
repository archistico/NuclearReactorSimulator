@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-closure-plan1-p1a"
set "NRS_M10_FINAL_REPLACEMENT_LONG_P1A=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P1A
echo ASYMPTOTIC CLOSURE EXTENSION
echo ============================================================
echo P2 Decision Gate 1 is VALIDATED.
echo Plan Amendment 1 authorizes only exact-v9 5.5/6 MWe.
echo P1 calibration and checkpoints must be reproduced.
echo Hard hold ceiling: 3600 s after each load command.
echo No production runtime, workload, authority, generator-load,
echo protection, exact-v9 or mission-pack change is authorized.
echo P1A returns only to P2R Decision Re-entry.
echo.
echo [1/2] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [2/2] Focused explicit P1A asymptotic closure extension...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongClosurePlan1P1AAsymptoticClosureExtensionTests.ExactV9_AsymptoticClosureExtension_ReproducesP1AndReturnsToP2R" --parallel none
if errorlevel 1 goto :fail

if not exist "%REPORT_DIR%\01-frozen-p1-calibration.csv" goto :missing
if not exist "%REPORT_DIR%\02-p1a-probe-summary.csv" goto :missing
if not exist "%REPORT_DIR%\03-p1a-events.csv" goto :missing
if not exist "%REPORT_DIR%\04-p1a-trajectories.csv" goto :missing
if not exist "%REPORT_DIR%\05-p1a-decision-summary.txt" goto :missing

echo.
echo M10 Final Replacement-Long Closure Plan 1 P1A completed.
echo Return the full "%REPORT_DIR%" folder before P2R branch selection, changing the replacement workload, changing runtime semantics, or freezing a second replacement-long baseline.
exit /b 0

:missing
echo.
echo M10 Final Replacement-Long Closure Plan 1 P1A FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final Replacement-Long Closure Plan 1 P1A FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
