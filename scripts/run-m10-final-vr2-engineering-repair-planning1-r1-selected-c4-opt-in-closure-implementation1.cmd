@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-r1-selected-c4-opt-in-closure-implementation1"
set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1="
set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1_ORDINARY_PASS="

echo ============================================================
echo M10 FINAL - VR2 R1 SELECTED C4 OPT-IN CLOSURE IMPLEMENTATION 1
echo ============================================================
echo Bounded production staging of the RP1C-selected C4 behavior behind explicit mode 2.
echo No default activation, exact-v9 activation, runtime override, R2 planning, VR3, P3-R1 or second replacement-long authorization.
echo.

echo [1/4] Static returned-planning, frozen-corpus, implementation-scope and payload audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-r1-selected-c4-opt-in-closure-implementation1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Ordinary Release suite with R1 opt-in unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :ordinary_fail

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%" >nul 2>nul
set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1=1"

echo.
echo [3/4] Forcing focused R1 test assembly rebuild from statically validated source...
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :focused_build_fail

echo.
echo [3/4] Focused R1 payload reproducibility, C4 equivalence and historical-mode regression...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.R1SelectedC4OptInClosureImplementationTests.R1SelectedC4_ProductionMode2_IsBitEquivalentReproducibleAndHistoricalModesRemainStable" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1="
for %%F in (03-reference-data-provenance.txt 04-c4-production-equivalence-summary.txt 05-historical-mode-regression-summary.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1_ORDINARY_PASS=1"
echo.
echo [4/4] R1 evidence integrity and bounded implementation adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-r1-selected-c4-opt-in-closure-implementation1.ps1"
if errorlevel 1 goto :adjudication_fail
set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1_ORDINARY_PASS="
for %%F in (01-contract-and-provenance.txt 02-production-change-manifest.csv 03-reference-data-provenance.txt 04-c4-production-equivalence-summary.txt 05-historical-mode-regression-summary.txt 06-r1-implementation-summary.txt 07-prequalification-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 R1 Selected C4 Opt-In Closure Implementation 1 completed.
echo Classification is implementation evidence only; this runner does not activate mode 2 or authorize R2 planning.
echo Return the full "%REPORT_DIR%" folder before R2 planning or any production-default/exact-v9 change.
exit /b 0

:ordinary_fail
echo.
echo R1 implementation FAILED ordinary Release regression. No downstream authority is granted.
exit /b 1
:focused_build_fail
set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1="
echo.
echo R1 implementation FAILED forced focused-project rebuild. No downstream authority is granted.
exit /b 1

:focused_fail
set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1="
echo.
echo R1 implementation FAILED focused equivalence/reproducibility evidence.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
:adjudication_fail
set "NRS_M10_FINAL_VR2_R1_IMPLEMENTATION1_ORDINARY_PASS="
echo.
echo R1 implementation evidence adjudication FAILED.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1
:missing
echo.
echo R1 implementation FAILED: required evidence artifact is missing.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1
:fail
echo.
echo R1 implementation FAILED before controlled evidence completion. No downstream authority is granted.
exit /b 1
