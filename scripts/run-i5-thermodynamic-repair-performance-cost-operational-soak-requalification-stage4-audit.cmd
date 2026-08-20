@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "OUT=artifacts\i5-thermodynamic-repair-performance-cost-operational-soak-stage4"

echo Running I.5 thermodynamic repair performance/cost/operational-soak requalification stage 4...
echo.
echo This keeps every registered/default runtime unchanged and benchmarks only the validated Hotfix 10 repair evidence seam.
echo Repaired explicit and repaired corrected use the same thermodynamic closure, so the H.28 relative ratios isolate corrected-ownership cost.
echo Original H.28 relative ceilings remain unchanged: median wall ratio ^<= 8, p95 wall ratio ^<= 12, median allocation ratio ^<= 16.
echo The 10 ms fixed step, H.20/H.22 fail-closed rules, H.9/P060-F040 and bounded hysteresis are not retuned.
echo.

if exist "%OUT%" rd /s /q "%OUT%"

dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-trait "Category=PhaseIThermodynamicRepairPerformanceCostOperationalSoakRequalification" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
  "01-i5-thermodynamic-repair-performance-cost-operational-soak-stage4.summary.txt"
  "02-repaired-performance-benchmark.csv"
  "03-repaired-operational-soak-samples.csv"
  "04-i5-thermodynamic-repair-performance-cost-operational-soak-stage4-metrics.csv"
) do (
  if not exist "%OUT%\%%~F" (
    echo ERROR: expected Stage 4 artifact missing: %%~F
    exit /b 2
  )
)

echo.
type "%OUT%\01-i5-thermodynamic-repair-performance-cost-operational-soak-stage4.summary.txt"
echo.
echo Detailed Stage 4 artifacts: "%CD%\%OUT%"
echo I.5 thermodynamic repair performance/cost/operational-soak requalification stage 4 completed.
exit /b 0
