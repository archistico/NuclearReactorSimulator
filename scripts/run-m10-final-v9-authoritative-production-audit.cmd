@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-v9-production-activation-decision"

echo Running M10 Final exact-v9 authoritative production audit...
echo Exact-v9 must be the authoritative default; exact-v4 remains historical and exact-v2 remains fail-closed.
echo Current production mission pack must be bounded-demand-following-5-10-5@3 while @1/@2 remain replayable.
echo.

set "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED=1"
set "NRS_M10_FINAL_V9_ACTIVATION_DECISION=1"
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalExactV9ProductionActivationDecisionTests.AuthoritativeExactV9_DefaultAndMissionPathsRemainHealthyConservativeDeterministicAndFailClosed" ^
  --parallel none
set "TEST_EXIT=%ERRORLEVEL%"
set "NRS_M10_FINAL_V9_ACTIVATION_DECISION="
set "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED="
if not "%TEST_EXIT%"=="0" exit /b %TEST_EXIT%

if not exist "%REPORT_DIR%\01-v9-production-activation-decision.summary.txt" (
  echo ERROR: expected exact-v9 authoritative activation summary missing.
  exit /b 2
)
if not exist "%REPORT_DIR%\02-selector-matrix.csv" (
  echo ERROR: expected selector matrix missing.
  exit /b 2
)
if not exist "%REPORT_DIR%\03-mission-pack-matrix.csv" (
  echo ERROR: expected mission-pack matrix missing.
  exit /b 2
)

echo.
type "%REPORT_DIR%\01-v9-production-activation-decision.summary.txt"
echo.
echo M10 Final exact-v9 authoritative production audit completed.
exit /b 0
