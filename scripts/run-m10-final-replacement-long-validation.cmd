@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-validation"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL EXACT-V9 REPLACEMENT LONG VALIDATION STARTED
copy /y "eng\m10-final-replacement-long-validation-contract.json" "%REPORT_DIR%\02-workload-contract.json" >nul
if errorlevel 1 exit /b 1

echo ============================================================
echo M10 FINAL EXACT-V9 REPLACEMENT LONG VALIDATION
echo ============================================================
echo Frozen workload: 1,920 simulated seconds / 192,000 authored deterministic 10 ms steps across five legs.
echo Authorized baseline: exact-v9 authoritative production + bounded-demand-following-5-10-5@3.
echo Target workstation wall time: 35-45 minutes. Hard campaign cap: 60 minutes.
echo The hard cap is validation-job policy, not a physics tolerance.
echo This script executes the authorized replacement long. It does NOT itself close M10.
echo.

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for replacement-long preflight/finalization.
  exit /b 1
)

echo [1/5] Validate returned baseline-freeze authorization, frozen manifests and single-test execution surface...
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-replacement-long-execution.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo execution-preflight=PASS

echo [2/5] Restore and build Debug warnings-as-errors...
dotnet restore
if errorlevel 1 exit /b 1
dotnet build --configuration Debug --no-restore -warnaserror
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo build=PASS

echo [3/5] Complete ordinary suite with replacement long remaining explicit-only...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo ordinary-suite=PASS

set "NRS_M10_FINAL_REPLACEMENT_LONG=1"
set "LONG_FAILURE=0"

echo [4/5] Execute authorized five-leg exact-v9 replacement long inside one 60-minute wall envelope...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongValidationTests.AuthorizedExactV9ReplacementLong_ExecutesFrozenFiveLegCampaignWithinWallBudget" --parallel none
if errorlevel 1 set "LONG_FAILURE=1"
set "NRS_M10_FINAL_REPLACEMENT_LONG="
if "%LONG_FAILURE%"=="0" >> "%REPORT_DIR%\00-progress.txt" echo replacement-long-test=PASS
if not "%LONG_FAILURE%"=="0" >> "%REPORT_DIR%\00-progress.txt" echo replacement-long-test=FAIL

echo [5/5] Finalize and validate replacement-long evidence...
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\finalize-m10-final-replacement-long-validation.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 set "LONG_FAILURE=1"
if "%LONG_FAILURE%"=="0" >> "%REPORT_DIR%\00-progress.txt" echo finalization=PASS
if not "%LONG_FAILURE%"=="0" >> "%REPORT_DIR%\00-progress.txt" echo finalization=FAIL

if not "%LONG_FAILURE%"=="0" (
  echo.
  echo M10 Final replacement long FAILED. M10 remains OPEN; inspect "%REPORT_DIR%".
  exit /b 1
)

for %%F in (
  "00-progress.txt"
  "01-m10-final-replacement-long-validation.summary.txt"
  "02-workload-contract.json"
  "03-leg-summary.csv"
  "04-conservation-maxima.csv"
  "05-healthy-window-v9-operating-point-sentinels.csv"
  "06-numerical-coupling-telemetry.csv"
  "07-trip-fault-protection-classification.csv"
  "08-mission-demand-score-evidence.csv"
  "09-replay-checkpoint-fingerprint-sentinels.csv"
  "10-evidence-growth.csv"
  "11-performance-diagnostics.csv"
  "12-wall-budget-summary.txt"
) do (
  if not exist "%REPORT_DIR%\%%~F" (
    echo ERROR: expected replacement-long artifact missing: %%~F
    exit /b 2
  )
)

echo.
echo M10 Final exact-v9 replacement long PASSED.
echo M10 closure is now eligible, but M11 must wait for explicit M10 closure documentation/promotion.
echo Return the full "%REPORT_DIR%" folder before closing M10.
exit /b 0
