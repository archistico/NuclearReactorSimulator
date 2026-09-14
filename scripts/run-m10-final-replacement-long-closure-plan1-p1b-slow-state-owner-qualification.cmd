@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-closure-plan1-p1b"
set "NRS_M10_FINAL_REPLACEMENT_LONG_P1B=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL REPLACEMENT-LONG CLOSURE PLAN 1 - P1B
echo SLOW-STATE CLOSURE / PHENOMENON-OWNER QUALIFICATION
echo ============================================================
echo Deep Review Pass 2 is locally VALIDATED / PASS-AS-AUTHORED.
echo P1B is observation-only and cannot select P3.
echo 5 MWe background reference: 600 s.
echo 5-to-6 MWe exact-v9 replay: hard ceiling 3600 s.
echo 1 s data are slow-state downsampling only; protection/numerical
echo sentinels are audited at canonical step/event cadence.
echo No production runtime, workload, authority, generator-load,
echo protection, exact-v9 or mission-pack change is authorized.
echo.
echo [1/2] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [2/2] Focused explicit P1B owner-localization gate...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongClosurePlan1P1BSlowStateOwnerQualificationTests.ExactV9_SlowStateClosureAndPhenomenonOwnerQualification_ReturnsEvidenceToP2R2" --parallel none
if errorlevel 1 goto :fail

for %%F in (
  01-background-reference-late-drift.csv
  02-p1a-checkpoint-reproduction.csv
  03-whole-domain-trajectory-1s.csv
  04-fluid-node-inventory-evidence.csv
  05-thermal-body-evidence.csv
  06-primary-drum-inventory-flow-evidence.csv
  07-primary-branch-flow-evidence.csv
  08-controller-diagnostics.csv
  09-actuator-command-physical-evidence.csv
  10-turbine-generator-balance-evidence.csv
  11-domain-late-window-trends.csv
  12-events-protection-numerical-sentinels.csv
  13-p1b-engineering-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final Replacement-Long Closure Plan 1 P1B completed.
echo Return the full "%REPORT_DIR%" folder before P2R2 branch selection, changing the replacement workload, changing runtime semantics, or freezing a second replacement-long baseline.
exit /b 0

:missing
echo.
echo M10 Final Replacement-Long Closure Plan 1 P1B FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final Replacement-Long Closure Plan 1 P1B FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
