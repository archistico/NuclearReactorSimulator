@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-requalification2-authored-seed-forward-inverse-consistency-diagnostic2"
set "NRS_M10_FINAL_VR2_R3_R2_SEED_CONSISTENCY_DIAGNOSTIC2="

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :cleanup_fail

echo ============================================================
echo M10 FINAL - VR2 R3 REQUALIFICATION 2 AUTHORED-SEED FORWARD / INVERSE CONSISTENCY DIAGNOSTIC 2
echo ============================================================
echo Diagnostic only. Reconstructs raw exact-v9 authored inventories before seed preconditioning.
echo No production repair, new exact-version identity, threshold change or R4 authority.
echo.

echo [1/4] Static returned-Diagnostic1, source/test and Diagnostic2 scope audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-requalification2-authored-seed-forward-inverse-consistency-diagnostic2.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Restore and forced no-incremental Application.Tests build...
dotnet restore
if errorlevel 1 goto :build_fail
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :build_fail

mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :create_fail

set "NRS_M10_FINAL_VR2_R3_R2_SEED_CONSISTENCY_DIAGNOSTIC2=1"
echo.
echo [3/4] Focused raw authored-seed forward/inverse and hydraulic-head diagnostic...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3Requalification2AuthoredSeedForwardInverseConsistencyDiagnostic2Tests.RawAuthoredExactV9Seed_IsCheckedForMode2ForwardInverseConsistencyBeforePreconditioning" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R3_R2_SEED_CONSISTENCY_DIAGNOSTIC2="

for %%F in (01-raw-authored-seed-closure-comparison.csv 02-raw-authored-seed-hydraulic-head-comparison.csv 03-diagnostic-summary.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo [4/4] Diagnostic 2 evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-requalification2-authored-seed-forward-inverse-consistency-diagnostic2.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (01-raw-authored-seed-closure-comparison.csv 02-raw-authored-seed-hydraulic-head-comparison.csv 03-diagnostic-summary.txt 04-pre-repair-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo R3 Requalification 2 Authored-Seed Forward / Inverse Consistency Diagnostic 2 completed.
echo Return the full "%REPORT_DIR%" folder before any repair or replanning decision.
exit /b 0

:cleanup_fail
echo Diagnostic 2 FAILED: stale artifact directory could not be removed.
exit /b 1
:create_fail
echo Diagnostic 2 FAILED: artifact directory could not be created.
exit /b 1
:build_fail
echo Diagnostic 2 FAILED forced Application.Tests build.
exit /b 1
:focused_fail
set "NRS_M10_FINAL_VR2_R3_R2_SEED_CONSISTENCY_DIAGNOSTIC2="
echo Diagnostic 2 focused test FAILED.
echo Preserve and return any files under "%REPORT_DIR%".
exit /b 1
:adjudication_fail
echo Diagnostic 2 evidence adjudication FAILED.
exit /b 1
:missing
echo Diagnostic 2 FAILED: required artifact missing.
exit /b 1
:fail
echo Diagnostic 2 FAILED before controlled evidence completion.
exit /b 1
