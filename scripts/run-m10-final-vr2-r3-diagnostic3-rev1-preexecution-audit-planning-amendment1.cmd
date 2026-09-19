@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
echo ============================================================
echo M10 FINAL - VR2 R3 DIAGNOSTIC 3 REV1 PREEXECUTION AUDIT / PLANNING AMENDMENT 1
echo ============================================================
echo Planning/amendment audit only. No build, runtime diagnostic, production repair, threshold change, R3 PASS or R4 authority.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-diagnostic3-rev1-preexecution-audit-planning-amendment1.ps1" || goto :fail
echo.
echo Diagnostic 3 REV1 Preexecution Audit / Planning Amendment 1 completed.
echo Next authorized activity: execute Diagnostic 3 REV1 test-only candidate.
exit /b 0
:fail
echo.
echo Diagnostic 3 REV1 Preexecution Audit / Planning Amendment 1 FAILED.
exit /b 1
