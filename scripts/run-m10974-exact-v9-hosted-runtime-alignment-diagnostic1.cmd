@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo ============================================================
echo M10.9.7.4 EXACT-V9 HOSTED RUNTIME ALIGNMENT DIAGNOSTIC 1
echo ============================================================
echo Static candidate validation only. Hosted execution is the experiment.
echo No production, test-source, golden, physics, tolerance or VR2/R3 change.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10974-exact-v9-hosted-runtime-alignment-diagnostic1.ps1"
if errorlevel 1 (
  echo.
  echo Exact-V9 Hosted Runtime Alignment Diagnostic 1 candidate validation FAILED.
  exit /b 1
)

echo.
echo Exact-V9 Hosted Runtime Alignment Diagnostic 1 candidate validation completed.
echo Push this exact candidate unchanged and inspect workflow exact-v9-runtime-alignment-diagnostic.
exit /b 0
