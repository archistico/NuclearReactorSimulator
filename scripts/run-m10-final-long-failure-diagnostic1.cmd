@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic1"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 1 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 1
echo ============================================================
echo Diagnostic only. No production tolerance, envelope or long acceptance criterion is changed.
echo Physical replay is capped at the already-validated exact-v4 300 s domain.
echo MISSION cost scaling is isolated with synthetic deterministic evidence; no multi-hour mission run is performed.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC=1"

echo [1/3] Build diagnostic test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/3] LR-H1 exact-v4 300 s equilibrium residual census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnosticTests.LR_H1_ExactV4_ThreeHundredSecondEquilibriumResidualCensus" --parallel none
if errorlevel 1 exit /b 1

echo [3/3] LR-M1 live mission projection prefix-scaling census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnosticTests.LR_M1_MissionProjectionPrefixScalingCensus" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC="

echo.
echo M10 final long failure diagnostic 1 completed.
echo Inspect "%REPORT_DIR%" and return the full artifact folder before authorizing any production correction.
exit /b 0
