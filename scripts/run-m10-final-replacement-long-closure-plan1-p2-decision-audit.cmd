@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P2 DECISION GATE 1
echo ============================================================
echo Documentation/planning-only decision audit. This does not execute,
echo repair, select P3, freeze or replace the replacement-long validation.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-replacement-long-closure-plan1-p2.ps1"
if errorlevel 1 exit /b 1
echo.
echo M10 Final Replacement-Long Closure Plan 1 P2 Decision Gate 1 completed.
endlocal
