@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-reference-consistent-raw-seed-candidate-construction1"
set "NRS_M10_FINAL_VR2_R3_RAW_SEED_CANDIDATE_CONSTRUCTION1="

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :cleanup_fail

echo ============================================================
echo M10 FINAL - VR2 R3 REFERENCE-CONSISTENT RAW-SEED CANDIDATE CONSTRUCTION 1
echo ============================================================
echo Test-only C4 conserved-inventory construction. Zero runtime/preconditioning steps and no production changes.
echo.

echo [1/4] Static returned-Replanning1, baseline, target-vector and construction-scope audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-reference-consistent-raw-seed-candidate-construction1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Restore and forced no-incremental Simulation.Tests build...
dotnet restore
if errorlevel 1 goto :build_fail
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :build_fail

mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :create_fail

set "NRS_M10_FINAL_VR2_R3_RAW_SEED_CANDIDATE_CONSTRUCTION1=1"
echo.
echo [3/4] Focused 12-node C4 candidate construction, target roundtrip and 8-head comparison...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2R3ReferenceConsistentRawSeedCandidateConstruction1Tests.FrozenExactV9TargetVector_HasDeterministicMode2ConservedInventoryCandidates" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R3_RAW_SEED_CANDIDATE_CONSTRUCTION1="

for %%F in (01-candidate-conserved-inventory-vector.csv 02-candidate-target-roundtrip.csv 03-candidate-hydraulic-head-comparison.csv 04-candidate-construction-summary.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo [4/4] Candidate-construction evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-reference-consistent-raw-seed-candidate-construction1.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (01-candidate-conserved-inventory-vector.csv 02-candidate-target-roundtrip.csv 03-candidate-hydraulic-head-comparison.csv 04-candidate-construction-summary.txt 05-pre-integration-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo R3 Reference-Consistent Raw-Seed Candidate Construction 1 completed.
echo Return the full "%REPORT_DIR%" folder before Seed Integration Planning 1.
exit /b 0

:cleanup_fail
echo Candidate Construction 1 FAILED: stale artifact directory could not be removed.
exit /b 1
:create_fail
echo Candidate Construction 1 FAILED: artifact directory could not be created.
exit /b 1
:build_fail
echo Candidate Construction 1 FAILED forced Simulation.Tests build.
exit /b 1
:focused_fail
set "NRS_M10_FINAL_VR2_R3_RAW_SEED_CANDIDATE_CONSTRUCTION1="
echo Candidate Construction 1 focused test FAILED.
echo Preserve and return any files under "%REPORT_DIR%".
exit /b 1
:adjudication_fail
echo Candidate Construction 1 evidence adjudication FAILED.
exit /b 1
:missing
echo Candidate Construction 1 FAILED: required artifact missing.
exit /b 1
:fail
echo Candidate Construction 1 FAILED before controlled evidence completion.
exit /b 1
