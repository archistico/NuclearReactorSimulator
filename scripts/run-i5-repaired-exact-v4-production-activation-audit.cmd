@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "OUT=artifacts\i5-repaired-exact-v4-production-activation"
if exist "%OUT%" rd /s /q "%OUT%"

echo Running I.5 repaired exact-v4 authoritative production activation audit...
echo.
echo This verifies the selector/scenario switch only. Repair Stages 1-4 and exact-v4 readiness are already validated.
echo Exact v2 remains fail-closed rollback, exact v3 remains historical/replayable, and the synchronization exact family is unchanged.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIRepairedExactVersion4ProductionActivationAuditTests.RepairedExactV4_IsAuthoritativeProductionWithV2FailClosedRollbackAndHistoricalV3Retention" ^
  --parallel none
if errorlevel 1 exit /b 1

if not exist "%OUT%\01-i5-repaired-exact-v4-production-activation.summary.txt" (
  echo ERROR: expected I.5 repaired exact-v4 production activation artifact missing.
  exit /b 2
)

echo.
type "%OUT%\01-i5-repaired-exact-v4-production-activation.summary.txt"
echo.
echo I.5 repaired exact-v4 production activation audit completed.
exit /b 0
