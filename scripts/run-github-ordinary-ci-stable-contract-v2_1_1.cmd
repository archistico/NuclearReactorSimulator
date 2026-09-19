@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo ============================================================
echo GITHUB ORDINARY CI STABLE CONTRACT V2.1.1 - HOSTED FAILURE CAPTURE
echo ============================================================
echo Harness/diagnostic-only. No production physics, test semantics, filter or threshold change.
echo.
echo [1/2] Static V2.1.1 contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-github-ordinary-ci-stable-contract-v2_1_1.ps1" || exit /b 1
echo.
echo [2/2] Full ordinary CI execution...
call eng\ci-ordinary.cmd || exit /b 1
echo.
echo GitHub Ordinary CI Stable Contract V2.1.1 completed.
echo Push this same candidate. Hosted failures publish ordinary-ci-diagnostics from the same test execution.
exit /b 0
