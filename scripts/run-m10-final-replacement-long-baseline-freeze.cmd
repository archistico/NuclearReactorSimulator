@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-baseline-freeze"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL EXACT-V9 REPLACEMENT-LONG BASELINE FREEZE STARTED
copy /y "eng\m10-final-replacement-long-validation-contract.json" "%REPORT_DIR%\02-replacement-long-contract.json" >nul
if errorlevel 1 exit /b 1

echo ============================================================
echo M10 FINAL EXACT-V9 REPLACEMENT-LONG BASELINE FREEZE
echo ============================================================
echo This gate freezes manifests and the redesigned workload only.
echo It does NOT execute the replacement long and does NOT close M10.
echo.

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for baseline freeze validation.
  exit /b 1
)

echo [1/5] Validate exact-v9 activation prerequisite, manifests and frozen workload...
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-replacement-long-baseline-freeze.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo freeze-contract-and-manifests=PASS

echo [2/5] Restore and build Debug warnings-as-errors...
dotnet restore
if errorlevel 1 exit /b 1
dotnet build --configuration Debug --no-restore -warnaserror
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo build=PASS

echo [3/5] Complete ordinary suite on authoritative exact-v9 source tree...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo ordinary-suite=PASS

echo [4/5] Re-run LR-M1 incremental semantic-equivalence/scaling regression...
set "NRS_M10_FINAL_LONG_DIAGNOSTIC2=1"
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic2Tests.LR_M1_IncrementalMissionProjectionScalingAndSemanticEquivalence" --parallel none
set "NRS_M10_FINAL_LONG_DIAGNOSTIC2="
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo lr-m1-hotfix1-regression=PASS

echo [5/5] Finalize baseline-freeze authorization record...
powershell -NoProfile -ExecutionPolicy Bypass -File "eng\finalize-m10-final-replacement-long-baseline-freeze.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo baseline-freeze=PASS

for %%F in (
  "00-progress.txt"
  "01-replacement-long-baseline-freeze.summary.txt"
  "02-replacement-long-contract.json"
  "03-manifest-summary.txt"
  "04-workstation-timing-plan.txt"
) do (
  if not exist "%REPORT_DIR%\%%~F" (
    echo ERROR: expected baseline-freeze artifact missing: %%~F
    exit /b 2
  )
)

echo.
echo M10 Final replacement-long baseline freeze completed.
echo Return the full "%REPORT_DIR%" folder before running the replacement long.
exit /b 0
