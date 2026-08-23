@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-v9-production-activation-decision"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL EXACT-V9 PRODUCTION ACTIVATION DECISION
echo ============================================================
echo Prerequisite: Diagnostic 11 Hotfix 2 exact-v9 qualification PASS.
echo Prerequisite: exact-v9 qualified opt-in activation candidate PASS.
echo This candidate switches the authoritative desktop default to exact-v9.
echo Exact-v4 remains historical/replayable; exact-v2 remains fail-closed.
echo Production mission @2 remains historical and new @3 binds exact-v9.
echo Replacement long remains unauthorized by this gate.
echo.

echo [1/6] Restore and build Debug warnings-as-errors...
dotnet restore
if errorlevel 1 exit /b 1
dotnet build --configuration Debug --no-restore -warnaserror
if errorlevel 1 exit /b 1

echo.
echo [2/6] Complete ordinary suite after authoritative switch...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo.
echo [3/6] LR-M1 Hotfix 1 semantic-equivalence regression on current production mission definition...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo [4/6] Re-run exact-v9 600 s whole-cycle qualification on the switched source tree...
set "NRS_M10_FINAL_LONG_DIAGNOSTIC11=1"
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic11Tests.LR_H1_ExactV9_SixHundredSecondPostMoistureEquilibriumRequalification" ^
  --parallel none
set "TEST_EXIT=%ERRORLEVEL%"
set "NRS_M10_FINAL_LONG_DIAGNOSTIC11="
if not "%TEST_EXIT%"=="0" exit /b %TEST_EXIT%

echo.
echo [5/6] Validate authoritative exact-v9 selector/scenario, historical exact-v4 retention, fail-closed exact-v2 and production mission @3...
call scripts\run-m10-final-v9-authoritative-production-audit.cmd
if errorlevel 1 exit /b 1

echo.
echo [6/6] Run post-switch cumulative current-evidence routing...
call eng\ci-current-evidence.cmd
if errorlevel 1 exit /b 1

if not exist "%REPORT_DIR%\01-v9-production-activation-decision.summary.txt" (
  echo ERROR: expected activation-decision summary missing.
  exit /b 2
)
if not exist "%REPORT_DIR%\02-selector-matrix.csv" (
  echo ERROR: expected selector matrix missing.
  exit /b 2
)
if not exist "%REPORT_DIR%\03-mission-pack-matrix.csv" (
  echo ERROR: expected mission-pack matrix missing.
  exit /b 2
)

copy /y "eng\m10-final-v9-production-activation-decision-contract.json" "%REPORT_DIR%\04-activation-decision-contract.json" >nul
if errorlevel 1 exit /b 1

>> "%REPORT_DIR%\00-progress.txt" echo build=PASS
>> "%REPORT_DIR%\00-progress.txt" echo ordinary-suite=PASS
>> "%REPORT_DIR%\00-progress.txt" echo lr-m1-hotfix1-regression=PASS
>> "%REPORT_DIR%\00-progress.txt" echo exact-v9-600s-requalification=PASS
>> "%REPORT_DIR%\00-progress.txt" echo v9-authoritative-policy-and-mission-path=PASS
>> "%REPORT_DIR%\00-progress.txt" echo post-switch-current-evidence=PASS

for %%F in (
  "00-progress.txt"
  "01-v9-production-activation-decision.summary.txt"
  "02-selector-matrix.csv"
  "03-mission-pack-matrix.csv"
  "04-activation-decision-contract.json"
) do (
  if not exist "%REPORT_DIR%\%%~F" (
    echo ERROR: expected activation-decision artifact missing: %%~F
    exit /b 2
  )
)

echo.
type "%REPORT_DIR%\01-v9-production-activation-decision.summary.txt"
echo.
echo M10 Final exact-v9 production activation decision completed.
echo Return the full "%REPORT_DIR%" folder before freezing the replacement-long baseline manifest or authorizing the replacement long.
exit /b 0
