@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo M10 FINAL - VR2 - R1 IMPLEMENTATION PLANNING 1
echo ============================================================
echo Planning only after returned RP1C SELECT-C4. No production source change or activation is performed.
echo.
echo [1/1] Static returned-selection, production-boundary and R1 contract audit...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-r1-implementation-planning1.ps1"
if errorlevel 1 goto :fail
echo.
echo M10 Final VR2 R1 Implementation Planning 1 completed.
echo Return the full ".\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-r1-implementation-planning1" folder before implementing R1 or opening R2 qualification planning.
exit /b 0
:fail
echo.
echo R1 Implementation Planning 1 FAILED.
echo No R1 implementation, production activation or downstream authority is granted.
exit /b 1
