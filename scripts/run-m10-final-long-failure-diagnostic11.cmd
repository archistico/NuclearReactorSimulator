@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic11"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 11 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 11 / EXACT-V9 POST-MOISTURE ANALYTICAL WHOLE-CYCLE REQUALIFICATION
echo ============================================================
echo Diagnostic 10 Hotfix 1 validated explicit moisture-drain ownership and removed turbine-inlet accumulation.
echo Exact-v8 remained off-root at about 4.868 MWe and +0.255 MW stored-energy drift.
echo Exact-v9 preserves exact-v8 semantics and recomputes only the authored post-moisture mass/energy operating point.
echo Exact-v4 remains authoritative production. Production activation and replacement long remain unauthorized.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC11=1"

echo [1/4] Build candidate test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/4] LR-M1 Hotfix 1 semantic-equivalence regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/4] LR-H1 exact-v9 600 s post-moisture whole-cycle equilibrium requalification...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic11Tests.LR_H1_ExactV9_SixHundredSecondPostMoistureEquilibriumRequalification" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC11="

echo.
echo M10 final long failure diagnostic 11 / exact-v9 post-moisture analytical whole-cycle requalification completed.
echo Return the full "%REPORT_DIR%" artifact folder before production activation, further operating-point changes or replacement long authorization.
exit /b 0
