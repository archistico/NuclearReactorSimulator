@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
if errorlevel 1 goto :fail

echo ============================================================
echo GITHUB ORDINARY CI - DETERMINISTIC SERIALIZATION HOTFIX 1
echo ============================================================
echo CI-harness-only validation. No retries, filters, skips or production/test changes.
echo.

echo [1/2] Static contract, returned-evidence and workflow audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-github-ordinary-ci-deterministic-hotfix1.ps1" || goto :fail

echo [2/2] Exact ordinary CI entry point...
call eng\ci-ordinary.cmd || goto :fail

echo.
echo GitHub Ordinary CI Deterministic Serialization Hotfix 1 completed.
echo Push the same candidate and confirm the hosted ordinary-ci workflow is green.
exit /b 0

:fail
echo.
echo GitHub Ordinary CI Deterministic Serialization Hotfix 1 FAILED.
exit /b 1
