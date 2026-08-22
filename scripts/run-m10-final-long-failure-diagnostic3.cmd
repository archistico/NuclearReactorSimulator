@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic3"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 3 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 3 / EXACT-V5 REFERENCE POINT
echo ============================================================
echo LR-M1 Hotfix 1 remains in place and is regression-checked.
echo LR-H1: exact-v5 is a distinct, non-production reference operating-point candidate.
echo Exact-v4 remains immutable and the authoritative production selector remains exact-v4.
echo No envelope, I.3 budget or conservation ceiling is widened.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC3=1"

echo [1/4] Build candidate test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/4] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/4] LR-H1 exact-v5 600 s reference operating-point census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic3Tests.LR_H1_ExactV5_ReferenceOperatingPointSixHundredSecondCensus" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC3="

echo.
echo M10 final long failure diagnostic 3 / exact-v5 reference operating-point candidate completed.
echo Return the full "%REPORT_DIR%" artifact folder before production activation or replacement long authorization.
exit /b 0
