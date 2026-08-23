@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P0 HOTFIX 2 AUDIT
echo ============================================================
echo Documentation/planning-only audit. This does not execute,
echo repair, freeze or replace the replacement-long validation.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-replacement-long-closure-plan1.ps1"
if errorlevel 1 exit /b %errorlevel%

echo.
echo M10 Final Replacement-Long Closure Plan 1 P0 Hotfix 2 audit completed.
exit /b 0
