@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "OUT=artifacts\i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2"

echo Running I.5 thermodynamic repair long-horizon/cross-profile requalification stage 2...
echo.
echo This keeps every registered/default runtime unchanged and runs only the Hotfix 10 repaired-closure evidence seam.
echo It reuses the validated H.19/H.24 30000-interval four-profile domain with real corrected-commit ownership,
echo checks fail-closed/conservation/determinism safety, and classifies post-startup continuity activity.
echo.

if exist "%OUT%" rd /s /q "%OUT%"

dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-trait "Category=PhaseIThermodynamicRepairLongHorizonCrossProfileRequalification" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
  "01-i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2.summary.txt"
  "02-repaired-long-horizon-step-telemetry.csv"
  "03-repaired-profile-qualification-metrics.csv"
  "04-i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2-metrics.csv"
) do (
  if not exist "%OUT%\%%~F" (
    echo ERROR: expected I.5 repair Stage-2 artifact missing: %%~F
    exit /b 2
  )
)

echo.
type "%OUT%\01-i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2.summary.txt"
echo.
echo Detailed CSV files: "%CD%\%OUT%"
echo I.5 thermodynamic repair long-horizon/cross-profile requalification stage 2 completed.
exit /b 0
