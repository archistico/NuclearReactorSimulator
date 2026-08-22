@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo PRE-M11 ENGINEERING REVIEW DOCUMENTATION CONSOLIDATION AUDIT
echo ============================================================
echo Planning/documentation-only audit. This does not execute or replace the M10 long validation.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-pre-m11-engineering-review-consolidation.ps1"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Pre-M11 engineering review documentation consolidation audit completed.
exit /b 0
