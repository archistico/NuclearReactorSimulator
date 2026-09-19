@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "OUT=%CD%\artifacts\m10-final-physical-reference-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2"
echo ============================================================
echo M10 FINAL - VR2 R3 DIAGNOSTIC 2 - ADJUDICATOR HOTFIX 1
echo ============================================================
echo Returned-evidence adjudication only. No restore, build, focused test, seed rerun or production change.
echo.
echo [1/2] Original candidate and returned-evidence provenance audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2-hotfix1.ps1" || goto :fail
if exist "%OUT%\07-diagnostic-summary.txt" del /q "%OUT%\07-diagnostic-summary.txt"
if exist "%OUT%\08-pre-repair-review.txt" del /q "%OUT%\08-pre-repair-review.txt"
if exist "%OUT%\09-adjudicator-hotfix1-record.txt" del /q "%OUT%\09-adjudicator-hotfix1-record.txt"
echo [2/2] Evidence adjudication with array-cardinality hotfix...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2-hotfix1.ps1" || goto :fail
for %%F in (01-raw-checkpoint-node-comparison.csv 02-seed-step1-node-comparison.csv 03-seed-step2-node-comparison.csv 04-seed-step-hydraulic-heads.csv 05-seed-step-flow-comparison.csv 06-seed-step-controller-turbine.csv 07-diagnostic-summary.txt 08-pre-repair-review.txt 09-adjudicator-hotfix1-record.txt) do if not exist "%OUT%\%%F" goto :fail
echo.
echo Diagnostic 2 Adjudicator Hotfix 1 completed.
echo Return the full "%OUT%" folder for engineering adjudication.
exit /b 0
:fail
echo Diagnostic 2 Adjudicator Hotfix 1 FAILED.
exit /b 1
