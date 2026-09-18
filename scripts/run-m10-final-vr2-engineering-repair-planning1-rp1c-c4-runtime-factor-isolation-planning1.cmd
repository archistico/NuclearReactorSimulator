@echo off
setlocal
cd /d "%~dp0.."

set ARTIFACT_DIR=artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation-planning1
if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 RUNTIME FACTOR ISOLATION PLANNING 1
echo ============================================================
echo Planning only on validated returned Attribution 1 evidence.
echo No factor-isolation implementation, PGO comparator, RP1C selection, production/runtime change,
echo threshold change, exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
echo.
echo [1/1] Static returned-evidence, single-factor matrix and authority-contract audit...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-factor-isolation-planning1.ps1"
if errorlevel 1 goto :fail

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C C4 Runtime Factor Isolation Planning 1 completed.
echo This freezes only the single-factor runtime-isolation design and evidence contract.
echo Return the full ".\%ARTIFACT_DIR%" folder before implementing Runtime Factor Isolation 1 or planning RP1C selection.
exit /b 0

:fail
echo.
echo Runtime Factor Isolation Planning 1 FAILED.
echo Preserve and return any files already written under ".\%ARTIFACT_DIR%".
exit /b 2
