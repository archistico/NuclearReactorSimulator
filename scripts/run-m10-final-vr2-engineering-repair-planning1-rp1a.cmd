@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1a"
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1A=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1A
echo ============================================================
echo Reference-domain corpus, seam-map and pre-candidate performance freeze.
echo No production repair, tolerance change, exact-v9 change, VR3,
echo P3-R1 execution or second replacement-long authorization.
echo.

echo [1/3] Static returned-planning prerequisite and RP1A contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1a.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/3] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [3/3] Focused explicit RP1A corpus, seam-map and current-performance freeze...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1aTests.Rp1a_FreezesReferenceCorpusSeamMapAndPreCandidatePerformanceCeilings" --parallel none
if errorlevel 1 goto :focused_fail

for %%F in (
  01-contract-and-provenance.txt
  02-vr2-reference-point-corpus.csv
  03-exact-v9-node-corpus.csv
  04-hydraulic-context.csv
  05-seam-probe-map.csv
  06-performance-baseline.csv
  07-rp1a-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1A completed.
echo This freezes corpus, seam evidence and pre-candidate performance ceilings only.
echo Return the full "%REPORT_DIR%" folder before implementing RP1B candidate families or changing production thermodynamics.
exit /b 0

:focused_fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1A did not meet its frozen execution contract.
echo Preserve and return the complete "%REPORT_DIR%" folder before RP1B or any production repair.
exit /b 1

:missing
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1A FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1A FAILED before focused completion.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
