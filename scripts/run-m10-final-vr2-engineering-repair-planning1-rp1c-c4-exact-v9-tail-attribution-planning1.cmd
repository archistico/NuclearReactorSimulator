@echo off
setlocal
cd /d "%~dp0.."

set ARTIFACT_DIR=artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution-planning1

if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 EXACT-V9 TAIL ATTRIBUTION PLANNING 1
echo ============================================================
echo Planning only on validated returned Full-Domain Performance Confirmation 1 evidence.
echo No attribution implementation, RP1C selection, production repair, threshold change,
echo exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
echo.
echo [1/1] Static returned-evidence, runtime-mode and tail-attribution planning-contract audit...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution-planning1.ps1"
if errorlevel 1 goto :fail

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C C4 Exact-v9 Tail Attribution Planning 1 completed.
echo This freezes only the evidence-only runtime attribution design and artifact contract.
echo Return the full ".\%ARTIFACT_DIR%" folder before implementing the attribution gate or planning RP1C selection.
exit /b 0

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C C4 Exact-v9 Tail Attribution Planning 1 FAILED.
echo Preserve and return any files already written under ".\%ARTIFACT_DIR%".
exit /b 2
