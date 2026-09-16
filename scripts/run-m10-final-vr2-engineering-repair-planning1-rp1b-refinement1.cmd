@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement1"
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT1=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 1
echo ============================================================
echo Test-only C2/D2 refinement against immutable RP1A and frozen RP1B evidence.
echo Candidate qualification is evidence only; RP1C performs any later selection.
echo No production repair, tolerance change, exact-v9 change, VR3,
echo P3-R1 execution or second replacement-long authorization.
echo.

echo [1/3] Static returned-RP1B prerequisite and Refinement 1 contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/3] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [3/3] Focused explicit RP1B Refinement 1 C2/D2 shadow candidate matrix...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement1Tests.Rp1bRefinement1_ProducesC2D2ShadowEvidenceAgainstFrozenRp1aCorpus" --parallel none
if errorlevel 1 goto :focused_fail

for %%F in (
  01-contract-and-provenance.txt
  02-candidate-vr2-error-map.csv
  03-candidate-exact-v9-node-map.csv
  04-candidate-seam-map.csv
  05-candidate-hydraulic-replay.csv
  06-candidate-performance.csv
  07-candidate-complexity.csv
  08-candidate-summary.csv
  09-rp1b-refinement1-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 1 completed.
echo This closes only the test-only C2/D2 refinement evidence matrix.
echo Return the full "%REPORT_DIR%" folder before implementing RP1C or changing production thermodynamics.
exit /b 0

:focused_fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 1 did not meet its frozen evidence-generation contract.
echo Preserve and return the complete "%REPORT_DIR%" folder before RP1C or any production repair.
exit /b 1

:missing
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 1 FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 1 FAILED before focused completion.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
