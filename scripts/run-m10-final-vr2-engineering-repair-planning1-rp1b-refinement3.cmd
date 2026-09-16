@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-refinement3"
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1B_REFINEMENT3=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B REFINEMENT 3
echo ============================================================
echo Immutable C3 performance-tail attribution and full worst-case qualification.
echo No C4, RP1C selection, production repair, tolerance change, exact-v9 change, VR3,
echo P3-R1 execution or second replacement-long authorization.
echo.

echo [1/3] Static returned-Refinement2 prerequisite and Refinement 3 contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1b-refinement3.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/3] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [3/3] Focused explicit C3 performance-tail attribution and full worst-case qualification...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1bRefinement3Tests.Rp1bRefinement3_AttributesAndQualifiesImmutableC3PerformanceTail" --parallel none
if errorlevel 1 goto :focused_fail

for %%F in (
  01-contract-and-provenance.txt
  02-c3-exact-v9-state-timing.csv
  03-c3-tail-target-repeats.csv
  04-c3-seam-side-timing.csv
  05-c3-performance-qualification.txt
  06-rp1b-refinement3-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 3 completed.
echo This closes only immutable-C3 performance-tail attribution evidence.
echo Return the full "%REPORT_DIR%" folder before RP1C, C4 or any production thermodynamic change.
exit /b 0

:focused_fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 3 did not meet its frozen evidence-generation contract.
echo Preserve and return the complete "%REPORT_DIR%" folder before RP1C, C4 or any production repair.
exit /b 1

:missing
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 3 FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1B Refinement 3 FAILED before focused completion.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
