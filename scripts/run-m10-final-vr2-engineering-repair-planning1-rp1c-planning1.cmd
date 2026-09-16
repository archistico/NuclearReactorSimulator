@echo off
setlocal
cd /d "%~dp0.."

set ARTIFACT_DIR=artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-planning1

if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C PLANNING 1
echo ============================================================
echo Planning only on validated returned C4 evidence.
echo No RP1C selection, production repair, threshold change, exact-v9 change,
echo VR3, P3-R1 or second replacement-long authorization.
echo.
echo [1/1] Static selection-input, corrected-predicate and remaining-evidence-gap audit...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-planning1.ps1"
if errorlevel 1 goto :fail

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C Planning 1 completed.
echo This freezes only the corrected readiness policy and C4 full-domain confirmation contract.
echo Return the full ".\%ARTIFACT_DIR%" folder before implementing the C4 full-domain confirmation gate.
exit /b 0

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C Planning 1 FAILED.
echo Preserve and return any files already written under ".\%ARTIFACT_DIR%".
exit /b 2
