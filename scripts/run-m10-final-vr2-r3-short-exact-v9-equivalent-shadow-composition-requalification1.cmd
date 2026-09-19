@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1"
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1="
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1_ORDINARY_PASS="

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :artifact_cleanup_fail

echo ============================================================
echo M10 FINAL - VR2 R3 SHORT EXACT-V9-EQUIVALENT SHADOW COMPOSITION REQUALIFICATION 1
echo ============================================================
echo Test-only single-factor composition qualification: canonical exact-v9 remains immutable; mode 2 is shadow-only.
echo No default activation, exact-v9 mutation, R4 execution, VR3, P3-R1 or second replacement-long authorization.
echo.

echo [1/5] Static returned-planning, baseline, exact-v9 provenance and execution-scope audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/5] Ordinary Release suite with R3 opt-in unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :ordinary_fail

mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :artifact_create_fail

echo.
echo [3/5] Forcing focused R3 Application test assembly rebuild from statically validated source...
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :focused_build_fail

set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1=1"
echo.
echo [4/5] Focused canonical-v9 baseline equivalence, mode-2 120 s health/ownership and deterministic-repeat qualification...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalificationTests.R3_Mode2Shadow_PreservesExactV9EquivalentShortHealthOwnershipAndDeterminism" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1="

for %%F in (02-shadow-baseline-equivalence.csv 03-mode2-shadow-health-trajectory.csv 04-ownership-conservation-summary.txt 05-deterministic-repeat.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1_ORDINARY_PASS=1"
echo.
echo [5/5] R3 evidence integrity and bounded requalification adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification1.ps1"
if errorlevel 1 goto :adjudication_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1_ORDINARY_PASS="

for %%F in (01-contract-and-provenance.txt 02-shadow-baseline-equivalence.csv 03-mode2-shadow-health-trajectory.csv 04-ownership-conservation-summary.txt 05-deterministic-repeat.txt 06-r3-requalification-summary.txt 07-prequalification-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 R3 Short Exact-v9-Equivalent Shadow Composition Requalification 1 completed.
echo Classification is R3 qualification evidence only; this runner does not activate mode 2 or authorize R4 planning.
echo Return the full "%REPORT_DIR%" folder before R4 planning or any production-default/exact-v9 change.
exit /b 0

:artifact_cleanup_fail
echo.
echo R3 requalification FAILED: stale R3 artifact directory could not be removed before gate execution.
exit /b 1

:artifact_create_fail
echo.
echo R3 requalification FAILED: R3 artifact directory could not be created.
exit /b 1

:ordinary_fail
echo.
echo R3 requalification FAILED ordinary Release regression. No downstream authority is granted.
exit /b 1

:focused_build_fail
echo.
echo R3 requalification FAILED forced focused-project rebuild. No downstream authority is granted.
exit /b 1

:focused_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1="
echo.
echo R3 requalification FAILED focused shadow-composition evidence.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1

:adjudication_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION1_ORDINARY_PASS="
echo.
echo R3 requalification evidence adjudication FAILED.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:missing
echo.
echo R3 requalification FAILED: required evidence artifact is missing.
exit /b 1

:fail
echo.
echo R3 requalification FAILED before controlled evidence completion. No downstream authority is granted.
exit /b 1
