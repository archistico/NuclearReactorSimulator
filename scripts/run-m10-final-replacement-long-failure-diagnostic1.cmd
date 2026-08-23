@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-failure-diagnostic1"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"
if errorlevel 1 exit /b 1

> "%REPORT_DIR%\00-progress.txt" echo M10 FINAL REPLACEMENT-LONG FAILURE DIAGNOSTIC 1 STARTED

echo ============================================================
echo M10 FINAL REPLACEMENT-LONG FAILURE DIAGNOSTIC 1
echo EXACT-V9 5-to-10 MWe PROTECTION PICKUP / LATCH OWNER CENSUS
echo ============================================================
echo The authorized replacement long completed all five legs in 35.25 minutes but remained RED.
echo RL-H1, RL-D1, RL-P1, wall budget, MISSION projection scaling and replay/checkpoint determinism were green.
echo RL-M1 and RL-R1 share the same post-load-raise protection trip. This gate changes no runtime source or workload.
echo.

set "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC1=1"

echo [1/4] Build diagnostic candidate...
dotnet build --configuration Debug -warnaserror
if errorlevel 1 exit /b 1

echo [2/4] Complete ordinary suite...
dotnet test --configuration Debug --no-build
if errorlevel 1 exit /b 1

echo [3/4] Preserve LR-M1 incremental projection regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10FinalLongMissionProjectionHotfixTests.*" --parallel none
if errorlevel 1 exit /b 1

echo [4/4] Reproduce exact-v9 5-to-10 MWe load raise and capture every protection pickup/latch for 10 simulated seconds...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongFailureDiagnostic1Tests.RL_M1_R1_ExactV9_LoadRaiseProtectionPickupAndLatchOwnerCensus" --parallel none
if errorlevel 1 exit /b 1

set "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC1="

echo.
echo M10 Final replacement-long failure Diagnostic 1 completed.
echo Return the full "%REPORT_DIR%" folder before changing the replacement workload, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
exit /b 0
