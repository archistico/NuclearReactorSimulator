@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1
echo ============================================================
echo Planning/design-selection audit only.
echo No production repair, tolerance change, exact-v9 change, VR3,
echo P3-R1 execution or second replacement-long authorization.
echo.

echo [1/1] Static returned-evidence, architecture and planning-contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1.ps1"
if errorlevel 1 goto :fail

echo.
echo M10 Final VR2 Engineering Repair Planning 1 completed.
echo Return the full ".\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1" folder before implementing RP1A or changing production thermodynamics.
exit /b 0

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 FAILED.
echo Preserve and return any files already written under ".\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1".
exit /b 2
