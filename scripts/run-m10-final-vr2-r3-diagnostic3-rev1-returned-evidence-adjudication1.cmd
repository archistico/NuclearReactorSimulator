@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo M10 FINAL - VR2 R3 DIAGNOSTIC 3 REV1 RETURNED-EVIDENCE ADJUDICATION 1
echo ============================================================
echo Returned-evidence audit only. No build, runtime diagnostic, repair planning, production repair, threshold change, R3 PASS or R4 authority.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-vr2-r3-diagnostic3-rev1-returned-evidence-adjudication1.ps1"
if errorlevel 1 (
  echo.
  echo Diagnostic 3 REV1 Returned-Evidence Adjudication 1 FAILED.
  exit /b 1
)
echo.
echo Diagnostic 3 REV1 Returned-Evidence Adjudication 1 completed.
echo Next authorized activity: confirm hosted GitHub ordinary-ci is GREEN.
exit /b 0
