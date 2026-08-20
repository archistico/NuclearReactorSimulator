@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "OUT=artifacts\i5-thermodynamic-repair-requalification-stage1"

echo Running I.5 thermodynamic repair requalification stage 1...
echo.
echo This keeps every registered/default runtime unchanged and runs only the Hotfix 10 repaired-closure evidence seam.
echo It applies the validated H.29 1024-interval control pattern under explicit and corrected hydraulics,
echo checks fail-closed/conservation/determinism safety, and classifies remaining branch-continuity activity.
echo.

if exist "%OUT%" rd /s /q "%OUT%"

dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-trait "Category=PhaseIThermodynamicRepairRequalificationStage1" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
  "01-i5-thermodynamic-repair-requalification-stage1.summary.txt"
  "02-repaired-corrected-step-telemetry.csv"
  "03-repaired-explicit-vs-corrected-comparison.csv"
  "04-i5-thermodynamic-repair-requalification-stage1-metrics.csv"
) do (
  if not exist "%OUT%\%%~F" (
    echo ERROR: expected I.5 repair requalification stage-1 artifact missing: %%~F
    exit /b 2
  )
)

echo.
type "%OUT%\01-i5-thermodynamic-repair-requalification-stage1.summary.txt"
echo.
echo Detailed CSV files: "%CD%\%OUT%"
echo I.5 thermodynamic repair requalification stage 1 completed.
exit /b 0
