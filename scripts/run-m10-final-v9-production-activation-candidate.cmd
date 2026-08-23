@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-v9-production-activation-candidate"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL EXACT-V9 PRODUCTION ACTIVATION CANDIDATE
 echo ============================================================
echo Exact-v9 is QUALIFIED by returned Diagnostic 11 Hotfix 2 evidence.
echo This gate stages exact-v9 as an explicit opt-in production policy only.
echo Authoritative production remains exact-v4; exact-v2 remains fail-closed kill/rollback.
echo Replacement long remains unauthorized.
echo.

echo [1/6] Restore and build Debug warnings-as-errors...
dotnet restore
if errorlevel 1 exit /b 1
dotnet build --configuration Debug --no-restore -warnaserror
if errorlevel 1 exit /b 1

echo.
echo [2/6] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo.
echo [3/6] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo.
echo [4/6] Re-run current evidence with exact-v4 still authoritative...
call eng\ci-current-evidence.cmd
if errorlevel 1 exit /b 1

echo.
echo [5/6] Re-run exact-v9 600 s qualification on the activation-candidate source tree...
set "NRS_M10_FINAL_LONG_DIAGNOSTIC11=1"
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic11Tests.LR_H1_ExactV9_SixHundredSecondPostMoistureEquilibriumRequalification" --parallel none
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_LONG_DIAGNOSTIC11="

echo.
echo [6/6] Validate exact-v9 opt-in production-policy path, fail-closed rollback and deterministic identity...
set "NRS_M10_FINAL_V9_PREREQUISITES_PASSED=1"
set "NRS_M10_FINAL_V9_ACTIVATION_CANDIDATE=1"
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalExactV9ProductionActivationCandidateTests.QualifiedExactV9_PolicyPathPreservesHealthConservationMoistureOwnershipAndDeterministicIdentity" --parallel none
if errorlevel 1 exit /b 1
set "NRS_M10_FINAL_V9_ACTIVATION_CANDIDATE="
set "NRS_M10_FINAL_V9_PREREQUISITES_PASSED="

if not exist "%REPORT_DIR%\01-v9-production-activation-candidate.summary.txt" (
  echo ERROR: expected activation-candidate summary missing.
  exit /b 2
)
if not exist "%REPORT_DIR%\02-selector-matrix.csv" (
  echo ERROR: expected selector matrix missing.
  exit /b 2
)
copy /y "eng\m10-final-v9-production-activation-candidate-contract.json" "%REPORT_DIR%\03-activation-candidate-contract.json" >nul
if errorlevel 1 exit /b 1
>> "%REPORT_DIR%\00-progress.txt" echo build=PASS
>> "%REPORT_DIR%\00-progress.txt" echo ordinary-suite=PASS
>> "%REPORT_DIR%\00-progress.txt" echo lr-m1-hotfix1-regression=PASS
>> "%REPORT_DIR%\00-progress.txt" echo current-v4-evidence=PASS
>> "%REPORT_DIR%\00-progress.txt" echo exact-v9-600s-requalification=PASS
>> "%REPORT_DIR%\00-progress.txt" echo v9-opt-in-policy-path=PASS

for %%F in (
  "00-progress.txt"
  "01-v9-production-activation-candidate.summary.txt"
  "02-selector-matrix.csv"
  "03-activation-candidate-contract.json"
) do (
  if not exist "%REPORT_DIR%\%%~F" (
    echo ERROR: expected activation-candidate artifact missing: %%~F
    exit /b 2
  )
)

echo.
type "%REPORT_DIR%\01-v9-production-activation-candidate.summary.txt"
echo.
echo M10 Final exact-v9 production activation candidate completed.
echo Return the full "%REPORT_DIR%" folder before switching the authoritative default, rebinding production mission packs, or authorizing the replacement long.
exit /b 0
