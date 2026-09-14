@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P2R / PLAN AMENDMENT 2 HOTFIX 1
echo ============================================================
echo Documentation/planning-only Hotfix 1. Validates Deep Review Pass 1 first,
echo then the P2R planning stop and Plan Amendment 2 contract.
echo.

call scripts\run-pre-m11-todreas-kazimi-deep-review-pass1-audit.cmd
if errorlevel 1 exit /b 1

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-replacement-long-closure-plan1-p2r-plan-amendment2.ps1"
if errorlevel 1 exit /b 1

echo.
echo M10 Final Replacement-Long Closure Plan 1 P2R / Plan Amendment 2 completed.
endlocal
