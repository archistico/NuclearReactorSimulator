@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-requalification2-initial-closure-operating-point-displacement-diagnostic1"
set "NRS_M10_FINAL_VR2_R3_R2_INITIAL_DISPLACEMENT_DIAGNOSTIC1="

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :cleanup_fail

echo ============================================================
echo M10 FINAL - VR2 R3 REQUALIFICATION 2 INITIAL CLOSURE / OPERATING-POINT DISPLACEMENT DIAGNOSTIC 1
echo ============================================================
echo Diagnostic only. Compares mode1/mode2 seed inventories, closure mapping, hydraulic heads and first 1 s dynamics.
echo No production repair, threshold change or R4 authority.
echo.

echo [1/4] Static returned-R3-2 failure, source/test and diagnostic-scope audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-requalification2-initial-closure-operating-point-displacement-diagnostic1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Restore and forced no-incremental Application.Tests build...
dotnet restore
if errorlevel 1 goto :build_fail
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :build_fail

mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :create_fail

set "NRS_M10_FINAL_VR2_R3_R2_INITIAL_DISPLACEMENT_DIAGNOSTIC1=1"
echo.
echo [3/4] Focused 12-node initial closure/head comparison and first 100-step side-by-side diagnostic...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3Requalification2InitialClosureOperatingPointDisplacementDiagnostic1Tests.ReturnedR3R2Red_IsLocalizedToInitialClosureMappingOrPostSeedDynamics" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R3_R2_INITIAL_DISPLACEMENT_DIAGNOSTIC1="

for %%F in (01-initial-node-closure-comparison.csv 02-initial-hydraulic-head-comparison.csv 03-first-second-dynamic-comparison.csv 04-diagnostic-summary.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo [4/4] Diagnostic evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-requalification2-initial-closure-operating-point-displacement-diagnostic1.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (01-initial-node-closure-comparison.csv 02-initial-hydraulic-head-comparison.csv 03-first-second-dynamic-comparison.csv 04-diagnostic-summary.txt 05-pre-repair-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo R3 Requalification 2 Initial Closure / Operating-Point Displacement Diagnostic 1 completed.
echo Diagnostic only. Return the full "%REPORT_DIR%" folder before any repair or replanning decision.
exit /b 0

:cleanup_fail
echo Diagnostic FAILED: stale artifact directory could not be removed.
exit /b 1
:create_fail
echo Diagnostic FAILED: artifact directory could not be created.
exit /b 1
:build_fail
echo Diagnostic FAILED forced Application.Tests build.
exit /b 1
:focused_fail
set "NRS_M10_FINAL_VR2_R3_R2_INITIAL_DISPLACEMENT_DIAGNOSTIC1="
echo Diagnostic focused test FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
:adjudication_fail
echo Diagnostic evidence adjudication FAILED.
exit /b 1
:missing
echo Diagnostic FAILED: required artifact missing.
exit /b 1
:fail
echo Diagnostic FAILED before controlled evidence completion.
exit /b 1
