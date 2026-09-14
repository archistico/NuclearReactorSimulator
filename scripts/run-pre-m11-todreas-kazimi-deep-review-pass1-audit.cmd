@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo PRE-M11 TODREAS/KAZIMI DEEP REVIEW PASS 1 - VALIDATOR HOTFIX 1
echo ============================================================
echo Documentation/planning-only pre-Plan-Amendment-2 evidence audit; validator Hotfix 1.
echo This does not execute a new post-P1A diagnostic, select P3,
echo change exact-v9/workload/protection, or authorize a second long.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-pre-m11-todreas-kazimi-deep-review-pass1.ps1"
if errorlevel 1 exit /b 1

echo.
echo Pre-M11 Todreas/Kazimi Deep Review Pass 1 completed.
endlocal
