@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo M10 FINAL - VR2 R3 DIAGNOSTIC 3 DEEP REVIEW ^& REV1 PLANNING 1
echo ============================================================
echo Planning/review audit only. No build, runtime diagnostic, production repair, threshold change, R3 PASS or R4 authority.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-diagnostic3-deep-review-rev1-planning1.ps1"
if errorlevel 1 (
  echo.
  echo Diagnostic 3 Deep Review ^& REV1 Planning 1 FAILED.
  exit /b 1
)
echo.
echo Diagnostic 3 Deep Review ^& REV1 Planning 1 completed.
echo Next authorized activity: implement Diagnostic 3 REV1 test-only candidate. Do not execute the reviewed Diagnostic 3 as authoritative evidence.
exit /b 0
