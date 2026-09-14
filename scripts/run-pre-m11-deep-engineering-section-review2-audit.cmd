@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo PRE-M11 DEEP ENGINEERING SECTION REVIEW 2
echo ============================================================
echo Documentation/planning-only audit. This does not execute P1A,
echo change runtime behavior, select P3 or authorize a second long.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-pre-m11-deep-engineering-section-review2.ps1"
if errorlevel 1 exit /b 1

echo.
echo Pre-M11 Deep Engineering Section Review 2 completed.
endlocal
