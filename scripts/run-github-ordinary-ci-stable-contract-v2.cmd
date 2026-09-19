@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo ============================================================
echo GITHUB ORDINARY CI STABLE CONTRACT V2
echo ============================================================
echo Harness-only stabilization. No production physics, test semantics, threshold, R3 PASS or repair authority.
echo.

echo [1/2] Static stable-contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-github-ordinary-ci-stable-contract-v2.ps1" || exit /b 1

echo [2/2] Full ordinary CI execution...
call eng\ci-ordinary.cmd || exit /b 1

echo.
echo GitHub Ordinary CI Stable Contract V2 completed.
echo Push this same candidate and confirm the hosted ordinary-ci workflow is GREEN.
exit /b 0
