@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\i5-repaired-v4-300s-reference-requalification"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running I.5 repaired exact-v4 300-second production-reference requalification...
echo.
echo Historical I.3 exact-v3 evidence and its 19 frozen budgets remain immutable provenance.
echo This gate runs authoritative exact @4 for 300 simulated seconds and compares final-window observations against those unchanged budgets.
echo It does not regenerate budgets, widen tolerances, retune physics or reinterpret exact @2/@3.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIRepairedExactVersion4ReferenceRequalificationAuditTests.HistoricalI3BudgetsRemainFrozenAndCurrentProductionContractIsRepairedExactV4" ^
  --parallel none
if errorlevel 1 exit /b 1

set "NRS_I5_REPAIRED_V4_300S_REFERENCE_AUDIT=1"
dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIRepairedExactVersion4ReferenceRequalificationAuditTests.AuthoritativeRepairedV4_ThreeHundredSeconds_RequalifiesFrozenI3BudgetsAndRemainsHealthy" ^
  --parallel none
set "EXITCODE=%ERRORLEVEL%"
set "NRS_I5_REPAIRED_V4_300S_REFERENCE_AUDIT="

if exist "%REPORT_DIR%\01-i5-repaired-v4-300s-reference-requalification.summary.txt" (
  echo.
  type "%REPORT_DIR%\01-i5-repaired-v4-300s-reference-requalification.summary.txt"
)
if not "%EXITCODE%"=="0" exit /b %EXITCODE%

for %%F in (
    "00-progress.txt"
    "01-i5-repaired-v4-300s-reference-requalification.summary.txt"
    "02-repaired-v4-reference-contract.csv"
    "03-repaired-v4-reference-trajectory-samples.csv"
    "04-repaired-v4-final-window-slopes.csv"
    "05-frozen-i3-budget-comparison.csv"
    "06-step-health-violations.csv"
    "07-targeted-reverse-flow-violations.csv"
    "08-production-telemetry.csv"
    "09-determinism-control.csv"
    "10-frozen-i3-tolerance-budgets.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected repaired-v4 reference artifact missing: %%~F
        exit /b 2
    )
)

echo.
echo Detailed repaired-v4 reference artifacts: "%REPORT_DIR%"
echo I.5 repaired exact-v4 300-second production-reference requalification completed.
exit /b 0
