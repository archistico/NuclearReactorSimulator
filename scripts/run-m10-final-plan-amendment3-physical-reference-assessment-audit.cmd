@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo M10 FINAL - PLAN AMENDMENT 3 / PHYSICAL REFERENCE ASSESSMENT
echo ============================================================
echo Documentation/planning-only audit.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-plan-amendment3-physical-reference-assessment.ps1"
if errorlevel 1 exit /b 1

echo.
echo M10 Final Plan Amendment 3 / Physical Reference Assessment completed.
exit /b 0
