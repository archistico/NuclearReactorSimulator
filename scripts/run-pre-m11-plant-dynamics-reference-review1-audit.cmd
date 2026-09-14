@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo PRE-M11 PLANT DYNAMICS / TH / REACTOR PHYSICS REVIEW 1
echo ============================================================
echo Documentation/planning-only audit. This does not execute P1A,
echo repair runtime behavior, select P3 or freeze a new long baseline.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-pre-m11-plant-dynamics-reference-review1.ps1"
if errorlevel 1 exit /b 1

echo.
echo Pre-M11 Plant Dynamics / TH / Reactor Physics Review 1 completed.
endlocal
