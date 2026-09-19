@echo off
setlocal
cd /d "%~dp0.."

set ARTIFACT_DIR=artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-amendment1-host-provenance
if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 RUNTIME CONFIGURATION IMPACT ASSESSMENT
echo PLANNING 1 AMENDMENT 1 - EXECUTION HOST PROVENANCE
echo ============================================================
echo Planning amendment only after returned Planning 1 PASS-AS-AUTHORED.
echo Freezes single-host execution provenance before any Branch A2 implementation.
echo No A2 implementation, FDPC2, RP1C selection, production/runtime change,
echo threshold change, exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
echo.
echo [1/1] Static returned-planning, host-provenance and authority-contract audit...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-amendment1-host-provenance.ps1"
if errorlevel 1 goto :fail

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C C4 Runtime Configuration Impact Assessment Planning 1 Amendment 1 completed.
echo This freezes only execution-host provenance and same-host interpretation rules for the future A2 gate.
echo Return the full ".\%ARTIFACT_DIR%" folder before implementing Runtime Configuration Impact Assessment 1.
exit /b 0

:fail
echo.
echo Runtime Configuration Impact Assessment Planning 1 Amendment 1 FAILED.
echo Preserve and return any files already written under ".\%ARTIFACT_DIR%".
exit /b 2
