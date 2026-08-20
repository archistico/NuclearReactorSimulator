@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPLAY=artifacts\i5-thermodynamic-repair-replay-checkpoint-protection-requalification-stage3"
set "OFFDESIGN=artifacts\i5-thermodynamic-repair-off-design-requalification-stage3"
set "OUT=artifacts\i5-thermodynamic-repair-requalification-stage3"

echo Running I.5 thermodynamic repair replay/checkpoint/protection + off-design requalification stage 3...
echo.
echo This keeps every registered/default runtime unchanged and exercises only the validated Hotfix 10 repair evidence seam.
echo Gate A requalifies recorder/full replay/checkpoint continuation and reverse-power protection.
echo Gate B replays the validated H.27 six-scenario bounded off-design envelope with real corrected ownership.
echo Performance/cost/soak is intentionally deferred to Stage 4 so semantic and timing failures remain separable.
echo.

if exist "%REPLAY%" rd /s /q "%REPLAY%"
if exist "%OFFDESIGN%" rd /s /q "%OFFDESIGN%"
if exist "%OUT%" rd /s /q "%OUT%"
mkdir "%OUT%" >nul 2>nul

echo [Stage 3A] Replay / checkpoint / protection...
dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-trait "Category=PhaseIThermodynamicRepairReplayCheckpointProtectionRequalification" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
  "01-i5-thermodynamic-repair-replay-checkpoint-protection-stage3.summary.txt"
  "02-repaired-replay-protection-trace.csv"
  "03-i5-thermodynamic-repair-replay-protection-stage3-metrics.csv"
) do (
  if not exist "%REPLAY%\%%~F" (
    echo ERROR: expected Stage 3 replay/protection artifact missing: %%~F
    exit /b 2
  )
  copy /y "%REPLAY%\%%~F" "%OUT%\%%~F" >nul
)

echo.
echo [Stage 3B] Off-design bounded envelope...
dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-trait "Category=PhaseIThermodynamicRepairOffDesignRequalification" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
  "04-i5-thermodynamic-repair-off-design-stage3.summary.txt"
  "05-repaired-off-design-step-telemetry.csv"
  "06-repaired-off-design-qualification-envelope.csv"
  "07-i5-thermodynamic-repair-off-design-stage3-metrics.csv"
) do (
  if not exist "%OFFDESIGN%\%%~F" (
    echo ERROR: expected Stage 3 off-design artifact missing: %%~F
    exit /b 2
  )
  copy /y "%OFFDESIGN%\%%~F" "%OUT%\%%~F" >nul
)

echo.
type "%OUT%\01-i5-thermodynamic-repair-replay-checkpoint-protection-stage3.summary.txt"
echo.
type "%OUT%\04-i5-thermodynamic-repair-off-design-stage3.summary.txt"
echo.
echo Combined Stage 3 artifacts: "%CD%\%OUT%"
echo I.5 thermodynamic repair requalification stage 3 completed.
exit /b 0
