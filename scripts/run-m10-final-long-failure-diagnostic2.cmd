@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-long-diagnostic2"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL LONG FAILURE DIAGNOSTIC 2 STARTED

echo ============================================================
echo M10 FINAL LONG FAILURE DIAGNOSTIC 2 / LR-M1 HOTFIX 1
echo ============================================================
echo LR-M1: validates exact-semantics incremental live demand evidence and bounded projection input.
echo LR-H1: correlates outlet inventory drift with canonical primary branch continuity and controller state over 300 s.
echo No thermodynamic envelope, I.3 budget, conservation ceiling or exact-version identity is changed.
echo.

set "NRS_M10_FINAL_LONG_DIAGNOSTIC2=1"

echo [1/5] Build diagnostic/hotfix test surface...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/5] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/5] Focused LR-M1 incremental semantic-equivalence tests...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/5] LR-M1 incremental projection scaling/equivalence census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic2Tests.LR_M1_IncrementalMissionProjectionScalingAndSemanticEquivalence" --parallel none
if errorlevel 1 exit /b 1

echo [5/5] LR-H1 exact-v4 primary branch/controller 300 s census...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalLongFailureDiagnostic2Tests.LR_H1_ExactV4_PrimaryBranchContinuityAndControllerCensus" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_LONG_DIAGNOSTIC2="

echo.
echo M10 final long failure diagnostic 2 / LR-M1 hotfix 1 completed.
echo Return the full "%REPORT_DIR%" artifact folder before starting the replacement long campaign.
exit /b 0
