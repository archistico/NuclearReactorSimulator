@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo PRE-M11 TODREAS/KAZIMI DEEP REVIEW PASS 2
echo ============================================================
echo Documentation/planning-only post-Plan-Amendment-2 audit.
echo This does not execute P1B, select P3, change production physics,
echo or authorize a second replacement-long baseline.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-pre-m11-todreas-kazimi-deep-review-pass2.ps1"
if errorlevel 1 exit /b 1

echo.
echo Pre-M11 Todreas/Kazimi Deep Review Pass 2 completed.
endlocal
