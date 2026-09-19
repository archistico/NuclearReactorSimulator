@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo M10 FINAL - VR2 R3 ENERGY-TRANSPORT OWNERSHIP REPAIR PLANNING 1
echo ============================================================
echo Planning/audit only. Family B authored for selection audit.
echo No production repair, retuning, threshold, C4 payload or R3 PASS.
echo.

set "ARTIFACT_DIR=%CD%\artifacts\m10-final-vr2-r3-energy-transport-ownership-repair-planning1"

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-energy-transport-ownership-repair-planning1.ps1" -Root "%CD%" -ArtifactDirectory "%ARTIFACT_DIR%"
if errorlevel 1 goto :fail

echo.
echo R3 Energy-Transport Ownership Repair Planning 1 completed.
echo Family B selected: ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT.
echo Return the complete folder:
echo %ARTIFACT_DIR%
echo Production remains unchanged; R3 remains RED.
exit /b 0

:fail
echo.
echo R3 Energy-Transport Ownership Repair Planning 1 FAILED.
exit /b 1
