@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P2R2 DECISION RE-ENTRY 2
echo ============================================================
echo Documentation/planning-only branch decision audit.
echo This does not implement P3-R1, repair runtime physics, change the workload,
echo or authorize a second replacement-long baseline.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-replacement-long-closure-plan1-p2r2-decision.ps1"
if errorlevel 1 exit /b 1

echo.
echo M10 Final Replacement-Long Closure Plan 1 P2R2 Decision Re-entry 2 completed.
endlocal
