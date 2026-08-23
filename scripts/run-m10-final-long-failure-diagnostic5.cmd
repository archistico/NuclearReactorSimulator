@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic5"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 5 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 5 / WHOLE-CYCLE STATE OWNER CENSUS
echo ============================================================
echo Diagnostic 3 Hotfix 1 completed, but exact-v5 is NOT QUALIFIED for production.
echo Exact-v5 is reused unchanged only to expose the complete authored thermofluid state behind the measured mass/energy owners.
echo Exact-v4 remains the authoritative production selector.
echo LR-M1 Hotfix 1 remains in place and is regression-checked.
echo No physical coefficient, controller tuning, acceptance budget or thermodynamic envelope is changed.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC5=1"

echo [1/4] Build candidate test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/4] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/4] LR-H1 exact-v5 600 s whole-cycle authored-state owner census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic5Tests.LR_H1_ExactV5_SixHundredSecondWholeCycleStateOwnerCensus" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC5="

echo.
echo M10 final long failure diagnostic 5 / exact-v5 whole-cycle state owner census completed.
echo Return the full "%REPORT_DIR%" artifact folder before authoring exact-v6, production activation, or replacement long authorization.
exit /b 0
