@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r2-focused-thermodynamic-reference-topology-qualification1"
set "NRS_M10_FINAL_VR2_R2_QUALIFICATION1="
set "NRS_M10_FINAL_VR2_R2_QUALIFICATION1_ORDINARY_PASS="
echo ============================================================
echo M10 FINAL - VR2 R2 FOCUSED THERMODYNAMIC REFERENCE TOPOLOGY QUALIFICATION 1
echo ============================================================
echo Test/reference-only qualification of explicit production mode 2 against independent IF97/RP1A evidence.
echo No default activation, exact-v9 composition, R3, VR3, P3-R1 or second replacement-long authorization.
echo.
echo [1/5] Static returned-planning, baseline, corpus and execution-scope audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r2-focused-thermodynamic-reference-topology-qualification1.ps1"
if errorlevel 1 goto :fail
echo.
echo [2/5] Ordinary Release suite with R2 opt-in unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :ordinary_fail
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%" >nul 2>nul
echo.
echo [3/5] Forcing focused R2 test assembly rebuild from statically validated source...
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :focused_build_fail
set "NRS_M10_FINAL_VR2_R2_QUALIFICATION1=1"
echo.
echo [4/5] Focused independent IF97, VR2, exact-v9 topology, seam continuity and deterministic-repeat qualification...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2R2FocusedThermodynamicReferenceTopologyQualificationTests.R2_Mode2_IsIndependentlyQualifiedAgainstIf97AndFrozenTopology" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R2_QUALIFICATION1="
for %%F in (02-reference-selfcheck.csv 03-vr2-reference-point-qualification.csv 04-exact-v9-topology-qualification.csv 05-seam-topology-continuity.csv 06-topology-qualification-summary.txt 07-deterministic-repeat.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing
set "NRS_M10_FINAL_VR2_R2_QUALIFICATION1_ORDINARY_PASS=1"
echo.
echo [5/5] R2 evidence integrity and bounded qualification adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r2-focused-thermodynamic-reference-topology-qualification1.ps1"
if errorlevel 1 goto :adjudication_fail
set "NRS_M10_FINAL_VR2_R2_QUALIFICATION1_ORDINARY_PASS="
for %%F in (01-contract-and-provenance.txt 02-reference-selfcheck.csv 03-vr2-reference-point-qualification.csv 04-exact-v9-topology-qualification.csv 05-seam-topology-continuity.csv 06-topology-qualification-summary.txt 07-deterministic-repeat.txt 08-r2-qualification-summary.txt 09-prequalification-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing
echo.
echo M10 Final VR2 R2 Focused Thermodynamic Reference Topology Qualification 1 completed.
echo Classification is R2 qualification evidence only; this runner does not activate mode 2 or authorize R3 planning.
echo Return the full "%REPORT_DIR%" folder before R3 planning or any production-default/exact-v9 change.
exit /b 0
:ordinary_fail
echo.
echo R2 qualification FAILED ordinary Release regression. No downstream authority is granted.
exit /b 1
:focused_build_fail
echo.
echo R2 qualification FAILED forced focused-project rebuild. No downstream authority is granted.
exit /b 1
:focused_fail
set "NRS_M10_FINAL_VR2_R2_QUALIFICATION1="
echo.
echo R2 qualification FAILED focused reference/topology evidence.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
:adjudication_fail
set "NRS_M10_FINAL_VR2_R2_QUALIFICATION1_ORDINARY_PASS="
echo.
echo R2 qualification evidence adjudication FAILED.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1
:missing
echo.
echo R2 qualification FAILED: required evidence artifact is missing.
exit /b 1
:fail
echo.
echo R2 qualification FAILED before controlled evidence completion. No downstream authority is granted.
exit /b 1
