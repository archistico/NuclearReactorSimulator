@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-materiality-diagnostic1"
set "NRS_M10_FINAL_PHYSICAL_REFERENCE_VR2_MATERIALITY_DIAGNOSTIC=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 REPLANNING / MATERIALITY DIAGNOSTIC 1
echo ============================================================
echo Observation-only exact-v9/P1B hydraulic materiality diagnostic.
echo IF97 is used offline on committed inventories; runtime state is unchanged.
echo No thermodynamic repair/tolerance change, exact-v9 change, VR3,
echo P3-R1 execution or second replacement-long authorization.
echo.

echo [1/3] Static returned-VR2 prerequisite and diagnostic contract audit...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-replanning-materiality-diagnostic1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/3] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [3/3] Focused explicit VR2 materiality diagnostic on exact-v9/P1B path...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalPhysicalReferenceVr2MaterialityDiagnosticTests.ExactV9_CompressedLiquidPressureDiscrepancy_IsMappedToCanonicalHydraulicMateriality" --parallel none
if errorlevel 1 goto :diagnostic_fail

for %%F in (
  01-contract-and-provenance.txt
  02-p1b-checkpoint-reproduction.csv
  03-node-if97-inverse-map.csv
  04-hydraulic-path-counterfactual.csv
  05-late-window-materiality.csv
  06-materiality-summary.txt
  07-sentinels.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 Replanning / Materiality Diagnostic 1 execution completed.
echo This completion is evidence collection only and does not authorize repair or VR3.
echo Return the full "%REPORT_DIR%" folder for engineering review before any thermodynamic repair/tolerance change, exact-v9 change, VR3, P3-R1, or second replacement-long execution.
exit /b 0

:diagnostic_fail
echo.
echo M10 Final VR2 Replanning / Materiality Diagnostic 1 did not meet its frozen execution contract.
echo Preserve and return the complete "%REPORT_DIR%" folder before any repair, tolerance change or VR3.
exit /b 1

:missing
echo.
echo M10 Final VR2 Replanning / Materiality Diagnostic 1 FAILED: expected artifacts are missing.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:fail
echo.
echo M10 Final VR2 Replanning / Materiality Diagnostic 1 FAILED before focused diagnostic completion.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
