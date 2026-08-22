@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "REPORT_DIR=%CD%\artifacts\m10-final-pre-m11-validation"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL PRE-M11 CUMULATIVE VALIDATION STARTED

echo ============================================================
echo M10 FINAL PRE-M11 CUMULATIVE VALIDATION
echo ============================================================
echo Scope: final curated non-regression gate over M10.9.8.5 VALIDATED / M10.9.8 CLOSED.
echo This is NOT the approximately-one-hour long gate; M10 remains OPEN after this script until run-m10-final-long-validation.cmd passes.
echo Historical superseded/frozen long audits remain provenance unless a current route below explicitly reruns them.
echo.

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for final V^&V contract validation.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10985-integrated-hmi-closure-contract.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-vv-matrix.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo contracts=PASS

echo [1/11] Restore...
dotnet restore
if errorlevel 1 exit /b 1

echo [2/11] Release build with warnings-as-errors...
dotnet build --configuration Release --no-restore -warnaserror
if errorlevel 1 exit /b 1

echo [3/11] Complete ordinary suite in Release...
dotnet test --configuration Release --no-build
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_ORDINARY_PASSED=1"
>> "%REPORT_DIR%\00-progress.txt" echo ordinary-release=PASS

echo [4/11] Debug build for focused --no-build gates...
dotnet build --configuration Debug --no-restore
if errorlevel 1 exit /b 1

echo [5/11] Current Phase-I / exact-v4 evidence...
call eng\ci-current-evidence.cmd
if errorlevel 1 exit /b 1
call scripts\run-i5-thermodynamic-repair-requalification-stage1-audit.cmd
if errorlevel 1 exit /b 1
call scripts\run-i5-thermodynamic-repair-replay-protection-off-design-requalification-stage3-audit.cmd
if errorlevel 1 exit /b 1
call scripts\run-i5-thermodynamic-repair-performance-cost-operational-soak-requalification-stage4-audit.cmd
if errorlevel 1 exit /b 1
call scripts\run-i5-repaired-v4-300s-reference-requalification-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_PHASEI_PASSED=1"
>> "%REPORT_DIR%\00-progress.txt" echo phase-i-current=PASS

echo [6/11] M10.9.5 contextual command closure...
call scripts\run-m1095-command-consequence-closure-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_M1095_PASSED=1"
>> "%REPORT_DIR%\00-progress.txt" echo m1095-current=PASS

echo [7/11] M10.9.6 challenge/replay closure...
call scripts\run-m1096-replay-checkpoint-closure-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_M1096_PASSED=1"
>> "%REPORT_DIR%\00-progress.txt" echo m1096-current=PASS

echo [8/11] M10.9.7 current persistence/host/timeline/closure...
call scripts\run-m10972-domain-definition-invariant-closure-audit.cmd --historical-reuse
if errorlevel 1 exit /b 1
call scripts\run-m10973-desktop-host-session-integrity-audit.cmd --historical-reuse
if errorlevel 1 exit /b 1
call scripts\run-m10974-mission-performance-timeline-audit.cmd --historical-reuse
if errorlevel 1 exit /b 1
call scripts\run-m1097-mission-performance-closure-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_M1097_PASSED=1"
>> "%REPORT_DIR%\00-progress.txt" echo m1097-current=PASS

echo [9/11] M10.9.8 healthy/degraded/replay matrices...
call scripts\run-m10982-healthy-assistance-authority-matrix-audit.cmd
if errorlevel 1 exit /b 1
call scripts\run-m10983-degraded-fault-protection-takeover-audit.cmd
if errorlevel 1 exit /b 1
call scripts\run-m10984-replay-checkpoint-same-seed-integrity-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_M1098_MATRICES_PASSED=1"
>> "%REPORT_DIR%\00-progress.txt" echo m1098-matrices=PASS

echo [10/11] M10.9.8 integrated HMI automated closure preflight rerun...
call scripts\run-m1098-integrated-human-automation-hmi-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_M1098_PASSED=1"
>> "%REPORT_DIR%\00-progress.txt" echo m1098-hmi=PASS

echo [11/11] Reference-plant scale current audit...
call scripts\run-reference-plant-scale-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_REFERENCE_SCALE_PASSED=1"
>> "%REPORT_DIR%\00-progress.txt" echo reference-scale=PASS

> "%REPORT_DIR%\01-m10-final-cumulative-validation.summary.txt" echo scope=M10 Final Pre-M11 Cumulative Validation over M10.9.8.5 VALIDATED / M10.9.8 CLOSED; curated current-authority rerun only; historical superseded long evidence remains provenance;
>> "%REPORT_DIR%\01-m10-final-cumulative-validation.summary.txt" echo m10985-manual-acceptance-recorded=True; m1098-validated-closed=True; vv-matrix-rows=27; frozen-i3-budgets=19;
>> "%REPORT_DIR%\01-m10-final-cumulative-validation.summary.txt" echo clean-restore=True; release-warnings-as-errors-build=True; complete-release-ordinary-suite=True; debug-focused-build=True;
>> "%REPORT_DIR%\01-m10-final-cumulative-validation.summary.txt" echo phase-i-current-evidence=True; exact-v4-300s-reference=True; m1095-closure=True; m1096-closure=True; m1097-current-closure=True; m1097-historical-exact-descriptor-checks-skipped=True; m1097-functional-owner-reruns=True; m1098-healthy-degraded-replay-hmi=True; reference-plant-scale=True;
>> "%REPORT_DIR%\01-m10-final-cumulative-validation.summary.txt" echo long-gate-executed=False; m10-final-cumulative-validation-passes=True; m10-closure-pending-long=True; next-step=run-m10-final-long-validation.cmd;

powershell -NoProfile -ExecutionPolicy Bypass -Command "$p='eng\m10-final-vv-matrix.json'; $s=[System.IO.File]::OpenRead($p); try {$h=[System.Security.Cryptography.SHA256]::Create(); try {$b=$h.ComputeHash($s)} finally {$h.Dispose()}} finally {$s.Dispose()}; ([System.BitConverter]::ToString($b)).Replace('-','').ToLowerInvariant()" > "%REPORT_DIR%\02-m10-final-vv-matrix.sha256.txt"
if errorlevel 1 exit /b 1
copy /y "eng\m10-final-vv-matrix.json" "%REPORT_DIR%\03-m10-final-vv-matrix.json" >nul
copy /y "eng\m10985-manual-acceptance-record.json" "%REPORT_DIR%\04-m10985-manual-acceptance-record.json" >nul

for %%F in (
  "00-progress.txt"
  "01-m10-final-cumulative-validation.summary.txt"
  "02-m10-final-vv-matrix.sha256.txt"
  "03-m10-final-vv-matrix.json"
  "04-m10985-manual-acceptance-record.json"
) do (
  if not exist "%REPORT_DIR%\%%~F" (
    echo ERROR: expected final cumulative artifact missing: %%~F
    exit /b 2
  )
)

echo.
echo === M10 Final Pre-M11 cumulative artifact summary ===
type "%REPORT_DIR%\01-m10-final-cumulative-validation.summary.txt"
echo.
echo M10 final cumulative validation completed.
echo M10 IS STILL OPEN: the separate long validation must pass before M11.
exit /b 0
