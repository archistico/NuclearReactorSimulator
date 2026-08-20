@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "OUT=artifacts\i5-repaired-exact-v4-activation-readiness"

if exist "%OUT%" rd /s /q "%OUT%"

echo Running I.5 repaired exact-v4 activation-readiness audit...
echo.
echo Exact @4 is registered as a distinct replayable repaired identity only.
echo Exact @2/@3 remain immutable and the current production selector intentionally remains @3 until the final activation/closure candidate.
echo The gate then drives @4 through the frozen 10 s steady -^> 30 s load raise -^> 30 s load lower journey.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-trait "Category=PhaseIRepairedExactVersion4ActivationReadinessAudit" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
  "01-i5-repaired-exact-v4-activation-readiness.summary.txt"
  "02-i5-repaired-v4-activation-readiness-checkpoints.csv"
) do (
  if not exist "%OUT%\%%~F" (
    echo ERROR: expected I.5 repaired exact-v4 activation-readiness artifact missing: %%~F
    exit /b 2
  )
)

echo.
type "%OUT%\01-i5-repaired-exact-v4-activation-readiness.summary.txt"
echo.
echo Detailed CSV files: "%CD%\%OUT%"
echo I.5 repaired exact-v4 activation-readiness audit completed.
exit /b 0
