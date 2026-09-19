@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C ENGINEERING REPAIR SELECTION 1
echo ============================================================
echo Decision-only gate on returned Selection Planning 1 and frozen FDPC2 evidence.
echo Authored decision: SELECT-C4. SELECT-NONE remains preserved as the mandatory alternative.
echo No new measurement, production repair, runtime change, threshold change, exact-v9 change, VR3,
echo P3-R1 or second replacement-long authorization.
echo.
echo [1/1] Static prerequisite, readiness, decision-policy and authority audit...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-selection1.ps1"
if errorlevel 1 goto :fail
echo.
echo M10 Final VR2 RP1C Engineering Repair Selection 1 completed.
echo Return the full ".\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-selection1" folder before R1 implementation planning or production repair.
exit /b 0
:fail
echo.
echo RP1C Engineering Repair Selection 1 FAILED.
echo No R1 planning or production repair authority is granted.
exit /b 1
