@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4"
set "C4_OPT=NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4"
set "C4_LANE=NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE"
set "C4_RUN=NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX"

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX="

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B C4
echo ============================================================
echo Separately versioned TEST-ONLY C4 implementation/evidence candidate.
echo C2/C3 remain immutable. No RP1C selection or production repair is authorized.
echo No threshold change, exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
echo.

echo [1/5] Static returned-planning authority, source pins and C4 implementation-contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1b-c4.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/5] Ordinary Release gate with C4 opt-in/lane/run variables unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%" >nul 2>nul

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4=1"
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX="

echo.
echo [3/5] Dedicated semantic-equivalence focused process...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests.Rp1bC4_EstablishesBitEquivalentSemanticsAcrossFrozenCorpus" --parallel none
if errorlevel 1 goto :focused_fail

for %%F in (
  02-state-semantic-equivalence.csv
  03-hydraulic-semantic-equivalence.csv
  04-semantic-equivalence-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo [4/5] Ten fresh timing processes: Lane A x5, then Lane B x5...
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE=A"
for /L %%R in (1,1,5) do (
  echo.
  echo ---- C4 Lane A process %%R of 5 ----
  set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX=%%R"
  dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests.Rp1bC4_MeasuresOneIndependentR1LaneRun" --parallel none
  if errorlevel 1 goto :focused_fail
)

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE=B"
for /L %%R in (1,1,5) do (
  echo.
  echo ---- C4 Lane B process %%R of 5 ----
  set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX=%%R"
  dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1bC4Tests.Rp1bC4_MeasuresOneIndependentR1LaneRun" --parallel none
  if errorlevel 1 goto :focused_fail
)

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_LANE="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4_RUN_INDEX="

for %%L in (a b) do (
  for /L %%R in (1,1,5) do (
    for %%F in (
      01-process-contract.txt
      02-c4-r1-call-timing.csv
      03-c4-r1-boundary-summary.csv
      04-runtime-context.txt
      05-process-summary.txt
    ) do if not exist "%REPORT_DIR%\lane-%%L\process-0%%R\%%F" goto :missing
  )
)

echo.
echo [5/5] Aggregate C4 evidence integrity and engineering classification...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1b-c4.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (
  01-contract-and-provenance.txt
  02-state-semantic-equivalence.csv
  03-hydraulic-semantic-equivalence.csv
  04-semantic-equivalence-summary.txt
  05-allocation-closure-summary.txt
  06-cross-process-run-summary.csv
  07-cross-process-boundary-summary.csv
  08-c4-evidence-adjudication.txt
  09-rp1b-c4-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_C4="

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B C4 completed.
echo C4 classification is evidence only; this runner does not authorize RP1C or production repair.
echo Return the full "%REPORT_DIR%" folder before any RP1C planning or production/runtime change.
exit /b 0

:focused_fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B C4 FAILED during a focused evidence process.
echo This indicates harness/evidence-integrity failure, not an engineering-negative C4 classification.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:adjudication_fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B C4 evidence adjudication FAILED integrity checks.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:missing
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B C4 FAILED: required evidence artifact is missing.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B C4 FAILED before focused evidence completion.
echo No RP1C or production/runtime authority is granted.
exit /b 1
