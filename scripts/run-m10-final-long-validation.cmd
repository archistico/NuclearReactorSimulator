@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-validation"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL PRE-M11 LONG VALIDATION STARTED
copy /y "eng\m10-final-long-validation-contract.json" "%REPORT_DIR%\02-workload-contract.json" >nul
if errorlevel 1 exit /b 1

echo ============================================================
echo M10 FINAL PRE-M11 LONG VALIDATION
echo ============================================================
echo Frozen workload: 14,400 simulated seconds / 1,440,000 deterministic 10 ms steps across five legs.
echo Wall-clock time is diagnostic only. The workload, I.3 budgets and conservation ceilings are frozen before this run.
echo This gate is separate from the already validated cumulative gate and is BLOCKING before M11.
echo.

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for long validation contract/finalization.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-long-validation-contract.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo contract=PASS

echo [1/7] Restore and compile explicit long-validation test surface...
dotnet restore
if errorlevel 1 exit /b 1
dotnet build --configuration Debug --no-restore -warnaserror
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo build=PASS

set "NRS_M10_FINAL_LONG_VALIDATION=1"
set "LONG_FAILURE=0"

echo [2/7] LR-H1 healthy exact-v4 7,200 s soak...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongValidationTests.LR_H1_HealthyExactV4_LongSoakPreservesConservationBudgetsAndNumericalSafety" --parallel none
if errorlevel 1 set "LONG_FAILURE=1"

echo [3/7] LR-M1 production mission @2 4,400 s continuation...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongValidationTests.LR_M1_ProductionMissionV2_LongContinuationPreservesDemandEvidenceAndPlantHealth" --parallel none
if errorlevel 1 set "LONG_FAILURE=1"

echo [4/7] LR-D1 degraded measurement / recovery 1,800 s...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongValidationTests.LR_D1_DegradedMeasurement_LongRecoveryRemainsFailClosedAndDeterministic" --parallel none
if errorlevel 1 set "LONG_FAILURE=1"

echo [5/7] LR-P1 protection / takeover 900 s...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongValidationTests.LR_P1_ProtectionAndTakeover_LongObservationPreservesProtectionPrecedence" --parallel none
if errorlevel 1 set "LONG_FAILURE=1"

echo [6/7] LR-R1 replay / checkpoint 100 s sentinel...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongValidationTests.LR_R1_ReplayCheckpoint_LongSentinelRemainsExactlyEquivalent" --parallel none
if errorlevel 1 set "LONG_FAILURE=1"

set "NRS_M10_FINAL_LONG_VALIDATION="

echo [7/7] Finalize and validate long evidence...
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\finalize-m10-final-long-validation.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 set "LONG_FAILURE=1"

if not "%LONG_FAILURE%"=="0" (
  echo.
  echo M10 final long validation FAILED. M10 remains OPEN; inspect "%REPORT_DIR%".
  exit /b 1
)

for %%F in (
  "00-progress.txt"
  "01-m10-final-long-validation.summary.txt"
  "02-workload-contract.json"
  "03-leg-summary.csv"
  "04-conservation-maxima.csv"
  "05-healthy-window-i3-budget-comparison.csv"
  "06-numerical-coupling-telemetry.csv"
  "07-trip-fault-protection-classification.csv"
  "08-mission-demand-score-evidence.csv"
  "09-replay-checkpoint-fingerprint-sentinels.csv"
  "10-evidence-growth.csv"
  "11-performance-diagnostics.csv"
) do (
  if not exist "%REPORT_DIR%\%%~F" (
    echo ERROR: expected final long artifact missing: %%~F
    exit /b 2
  )
)

echo.
echo M10 final long validation PASSED.
echo M10 closure is now eligible, but M11 must wait for explicit M10 closure documentation/promotion.
exit /b 0
