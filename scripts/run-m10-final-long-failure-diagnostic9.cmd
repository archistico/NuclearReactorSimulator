@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic9"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 9 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 9 / EXACT-V7 TURBINE-ADMISSION MASS-OWNER CENSUS
echo ============================================================
echo Diagnostic 8 execution passed but exact-v7 remains engineering NOT QUALIFIED.
echo This candidate changes no runtime source and creates no exact-v8.
echo It freezes admission-stage-condenser-hotwell-feedwater mass ownership before any turbine semantic repair.
echo Exact-v4 remains authoritative production. Production activation and replacement long remain unauthorized.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC9=1"

echo [1/4] Build candidate test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/4] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/4] LR-H1 exact-v7 180 s turbine-admission and closed-cycle mass-owner census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic9Tests.LR_H1_ExactV7_TurbineAdmissionAndClosedCycleMassOwnerCensus" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC9="

echo.
echo M10 final long failure diagnostic 9 / exact-v7 turbine-admission mass-owner census completed.
echo Return the full "%REPORT_DIR%" artifact folder before authoring exact-v8, changing turbine-admission semantics, production activation or replacement long authorization.
exit /b 0
