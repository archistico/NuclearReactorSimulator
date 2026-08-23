@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic6"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 6 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 6 / ANALYTICAL WHOLE-CYCLE EQUILIBRIUM CANDIDATE
echo ============================================================
echo Diagnostic 5 completed and provides the whole-cycle owner evidence used to author exact-v6.
echo Exact-v6 is a distinct analytical authored-state candidate; exact-v4 remains production and exact-v5 remains failed diagnostic evidence.
echo Exact-v4 remains the authoritative production selector.
echo LR-M1 Hotfix 1 remains in place and is regression-checked.
echo No physical coefficient, controller gain, acceptance budget or thermodynamic envelope is changed; only authored initial state and equilibrium controller/pump biases are candidate changes.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC6=1"

echo [1/4] Build candidate test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/4] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/4] LR-H1 exact-v6 600 s analytical whole-cycle equilibrium census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic6Tests.LR_H1_ExactV6_SixHundredSecondWholeCycleEquilibriumCensus" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC6="

echo.
echo M10 final long failure diagnostic 6 / exact-v6 analytical whole-cycle equilibrium candidate completed.
echo Return the full "%REPORT_DIR%" artifact folder before production activation or replacement long authorization.
exit /b 0
