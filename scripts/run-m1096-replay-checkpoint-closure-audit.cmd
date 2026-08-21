@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running cumulative M10.9.6 replay/checkpoint/determinism closure...
echo.
echo [1/5] M10.9.6.1 lifecycle/logical-time prerequisite
call scripts\run-m1096-challenge-lifecycle-audit.cmd
if errorlevel 1 exit /b 1

echo.
echo [2/5] M10.9.6.2 external-demand prerequisite
call scripts\run-m1096-external-energy-demand-audit.cmd
if errorlevel 1 exit /b 1

echo.
echo [3/5] M10.9.6.3 multidimensional-scoring prerequisite
call scripts\run-m1096-multidimensional-scoring-audit.cmd
if errorlevel 1 exit /b 1

echo.
echo [4/5] M10.9.6.4 initial challenge-pack prerequisite
call scripts\run-m1096-initial-challenge-pack-audit.cmd
if errorlevel 1 exit /b 1

echo.
echo [5/5] M10.9.6.5 replay/checkpoint/determinism closure
if exist "artifacts\m1096-closure" rd /s /q "artifacts\m1096-closure"
dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Replay.M10965ChallengeReplayCheckpointClosureTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.6.5 closure artifact summary ===
if exist "artifacts\m1096-closure\01-m1096-replay-checkpoint-determinism-closure.summary.txt" (
  type "artifacts\m1096-closure\01-m1096-replay-checkpoint-determinism-closure.summary.txt"
) else (
  echo ERROR: expected M10.9.6.5 summary artifact was not written.
  exit /b 1
)
if not exist "artifacts\m1096-closure\02-m1096-closure-gate-matrix.csv" exit /b 1
if not exist "artifacts\m1096-closure\03-m1096-pack-identity-policy-matrix.csv" exit /b 1

echo.
echo Automated M10.9.6 closure gate completed.
echo Final promotion requires: docs\M10_9_6_5_MANUAL_VALIDATION_CHECKLIST.md
exit /b 0
