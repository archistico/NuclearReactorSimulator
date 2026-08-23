@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic8"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 8 STARTED

 echo ============================================================
 echo M10 FINAL LONG FAILURE DIAGNOSTIC 8 / EXACT-V7 GRID-DROOP INTEGRAL-REFERENCE REQUALIFICATION
 echo ============================================================
 echo Diagnostic 7 proved that the historical breaker-closed governor integrates the intentional droop offset.
 echo Exact-v7 preserves the exact-v6 analytical whole-cycle authored state and opts into the versioned synchronous integral reference.
 echo Exact-v4 remains the authoritative production selector. Exact-v5 and exact-v6 remain frozen diagnostic evidence.
 echo Production activation and replacement long remain unauthorized.
 echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC8=1"

 echo [1/4] Build candidate test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

 echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

 echo [3/4] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

 echo [4/4] LR-H1 exact-v7 600 s whole-cycle + governor integral-reference requalification...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic8Tests.LR_H1_ExactV7_SixHundredSecondGridDroopIntegralReferenceRequalification" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC8="

 echo.
 echo M10 final long failure diagnostic 8 / exact-v7 grid-droop integral-reference requalification completed.
 echo Return the full "%REPORT_DIR%" artifact folder before production activation or replacement long authorization.
exit /b 0
