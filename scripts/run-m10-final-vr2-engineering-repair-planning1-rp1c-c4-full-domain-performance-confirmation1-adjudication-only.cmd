@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
set "REPORT_DIR=artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 FULL-DOMAIN PERFORMANCE CONFIRMATION 1 - HOTFIX 3 ADJUDICATION ONLY
echo ============================================================
echo Reuses completed immutable per-process evidence only.
echo No build, no focused rerun, no RP1C selection and no production/runtime change.
echo.

if not exist "%REPORT_DIR%" goto :missing

for /L %%R in (1,1,5) do (
  for %%F in (
    01-process-contract.txt
    02-exact-v9-call-timing.csv
    03-seam-call-timing.csv
    04-runtime-context.txt
    05-process-summary.txt
  ) do if not exist "%REPORT_DIR%\process-0%%R\%%F" goto :missing
)

echo [1/1] Aggregate existing complete process evidence and classify full-domain confirmation...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1.ps1"
if errorlevel 1 goto :fail

for %%F in (
  01-contract-and-provenance.txt
  02-cross-process-run-summary.csv
  03-cross-process-seam-side-summary.csv
  04-confirmation-evidence-adjudication.txt
  05-rp1c-c4-full-domain-performance-confirmation1-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

for /f %%C in ('dir /b /s /a-d "%REPORT_DIR%" ^| find /c /v ""') do set "FILE_COUNT=%%C"
if not "%FILE_COUNT%"=="30" goto :count_mismatch

echo.
echo Full-Domain Performance Confirmation 1 Hotfix 3 adjudication-only completed.
echo Return the complete "%REPORT_DIR%" folder for engineering adjudication.
exit /b 0

:count_mismatch
echo.
echo Adjudication-only FAILED: expected 30 files, found %FILE_COUNT%.
exit /b 1

:missing
echo.
echo Adjudication-only FAILED: required completed process evidence is missing.
echo Do not rerun focused evidence automatically; preserve and return the current artifact folder.
exit /b 1

:fail
echo.
echo Adjudication-only FAILED integrity checks.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1
