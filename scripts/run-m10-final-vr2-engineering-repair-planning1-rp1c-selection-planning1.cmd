@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C SELECTION PLANNING 1
echo ============================================================
echo Planning only on returned FDPC2 evidence. No selection or production repair is performed.
echo.
echo [1/1] Static FDPC2 evidence, readiness-matrix and selection-contract audit...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-selection-planning1.ps1"
if errorlevel 1 goto :fail
echo.
echo M10 Final VR2 RP1C Selection Planning 1 completed.
echo Return the full ".\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-selection-planning1" folder before implementing RP1C selection or production repair.
exit /b 0
:fail
echo.
echo RP1C Selection Planning 1 FAILED.
echo No RP1C selection or production authority is granted.
exit /b 1
