@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement5"
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 5
echo ============================================================
echo Immutable C3 cross-process R1-seam wall-clock tail reproducibility.
echo Five independent focused-test processes; same boundary must exceed in at least two processes to justify C4 planning.
echo No C4 implementation, RP1C selection, production repair, tolerance change, exact-v9 change, VR3,
echo P3-R1 execution or second replacement-long authorization.
echo.

echo [1/4] Static returned-performance-replanning prerequisite and Refinement 5 contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement5.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [3/4] Five independent focused C3 R1-seam timing processes...
for /L %%R in (1,1,5) do (
  echo.
  echo ---- Refinement 5 independent process %%R of 5 ----
  set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5_RUN_INDEX=%%R"
  dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement5Tests.Rp1bRefinement5_MeasuresImmutableC3R1SeamAcrossOneIndependentProcessRun" --parallel none
  if errorlevel 1 goto :focused_fail
)
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT5_RUN_INDEX="

for /L %%R in (1,1,5) do (
  set "RUN_DIR=%REPORT_DIR%\process-0%%R"
  for %%F in (
    01-process-contract.txt
    02-c3-r1-call-timing.csv
    03-c3-r1-boundary-summary.csv
    04-runtime-context.txt
    05-process-summary.txt
  ) do if not exist "%REPORT_DIR%\process-0%%R\%%F" goto :missing
)

echo.
echo [4/4] Cross-process same-boundary reproducibility adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement5.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (
  01-contract-and-provenance.txt
  02-cross-process-run-summary.csv
  03-cross-process-boundary-summary.csv
  04-performance-reproducibility-adjudication.txt
  05-rp1b-refinement5-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 5 completed.
echo This closes only immutable-C3 cross-process wall-clock tail reproducibility evidence.
echo Return the full "%REPORT_DIR%" folder before any C4 planning, performance-contract adjudication or RP1C.
exit /b 0

:focused_fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 5 failed during an independent focused process.
echo Preserve and return the complete "%REPORT_DIR%" folder before any C4, RP1C or production repair.
exit /b 1

:adjudication_fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 5 generated process evidence but cross-process adjudication FAILED.
echo Preserve and return the complete "%REPORT_DIR%" folder before any C4, RP1C or production repair.
exit /b 1

:missing
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 5 FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 5 FAILED before focused completion.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
