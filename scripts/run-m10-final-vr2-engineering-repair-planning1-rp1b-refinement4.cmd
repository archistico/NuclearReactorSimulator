@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement4"
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT4=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 4
echo ============================================================
echo Immutable C3 R1-seam worst-case localization and reproducibility only.
echo No C4, RP1C selection, production repair, tolerance change, exact-v9 change, VR3,
echo P3-R1 execution or second replacement-long authorization.
echo.

echo [1/3] Static returned-Refinement3 prerequisite and Refinement 4 contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement4.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/3] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [3/3] Focused explicit immutable-C3 R1-seam localization and reproducibility diagnostic...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement4Tests.Rp1bRefinement4_LocalizesAndTestsReproducibilityOfImmutableC3R1SeamWorstCase" --parallel none
if errorlevel 1 goto :focused_fail

for %%F in (
  01-contract-and-provenance.txt
  02-c3-r1-seam-call-timing.csv
  03-c3-r1-boundary-summary.csv
  04-c3-r1-target-repeats.csv
  05-c3-r1-runtime-context.txt
  06-c3-r1-performance-attribution.txt
  07-rp1b-refinement4-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 4 completed.
echo This closes only immutable-C3 R1-seam localization and reproducibility evidence.
echo Return the full "%REPORT_DIR%" folder before RP1C, C4 or any production thermodynamic change.
exit /b 0

:focused_fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 4 did not meet its frozen evidence-generation contract.
echo Preserve and return the complete "%REPORT_DIR%" folder before RP1C, C4 or any production repair.
exit /b 1

:missing
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 4 FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 4 FAILED before focused completion.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
