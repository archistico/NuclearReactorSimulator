@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic10"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 10 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 10 / EXACT-V8 TURBINE MOISTURE-DRAIN REQUALIFICATION
echo ============================================================
echo Diagnostic 9 proved that rejected non-vapor stage mass remains in turbine-inlet.
echo Exact-v8 preserves exact-v7 authored state and governor repair, but versions turbine admission ownership.
echo Vapor drives the turbine/exhaust path; rejected moisture drains explicitly to hotwell with conservative energy ownership.
echo Exact-v4 remains authoritative production. Production activation and replacement long remain unauthorized.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC10=1"

echo [1/4] Build candidate test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/4] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/4] LR-H1 exact-v8 600 s turbine moisture-drain whole-cycle requalification...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic10Tests.LR_H1_ExactV8_SixHundredSecondMoistureDrainRequalification" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC10="

echo.
echo M10 final long failure diagnostic 10 / exact-v8 turbine moisture-drain requalification completed.
echo Return the full "%REPORT_DIR%" artifact folder before production activation, further operating-point changes or replacement long authorization.
exit /b 0
