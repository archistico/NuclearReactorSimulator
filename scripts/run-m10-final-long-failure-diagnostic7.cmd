@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic7"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 7 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 7 / GOVERNOR-DROOP STEAM-PATH OWNER CENSUS
echo ============================================================
echo Diagnostic 6 execution passed but exact-v6 remains engineering NOT QUALIFIED.
echo The returned evidence shows persistent pressure/inventory drift and electrical export movement from ~5.0 to ~5.2 MWe.
echo Diagnostic 7 changes no runtime source and authors no exact-v7.
echo It freezes governor/droop, valve-flow, stage-flow and steam-path inventory ownership over 180 simulated seconds.
echo Exact-v4 remains the authoritative production selector. Replacement long remains unauthorized.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC7=1"

echo [1/4] Build candidate test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/4] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/4] LR-H1 exact-v6 180 s governor/droop + steam-path owner census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic7Tests.LR_H1_ExactV6_GovernorDroopSteamPathOwnerCensus" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC7="

echo.
echo M10 final long failure diagnostic 7 / exact-v6 governor-droop steam-path owner census completed.
echo Return the full "%REPORT_DIR%" artifact folder before authoring exact-v7, production activation or replacement long authorization.
exit /b 0
