@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification2"
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION2="
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION2_ORDINARY_PASS="
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :artifact_cleanup_fail
echo ============================================================
echo M10 FINAL - VR2 R3 SHORT EXACT-V9-EQUIVALENT SHADOW COMPOSITION REQUALIFICATION 2
echo ============================================================
echo Re-run on returned/adjudicated fusion-repaired production baseline.
echo.
echo [1/6] Static repair-evidence, repaired-baseline, exact-v9 provenance and execution-scope audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification2.ps1"
if errorlevel 1 goto :fail
echo.
echo [2/6] Full no-incremental Release build before ordinary suite...
dotnet restore
if errorlevel 1 goto :preordinary_build_fail
dotnet build --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :preordinary_build_fail
echo.
echo [3/6] Ordinary Release suite with R3-2 opt-in unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :ordinary_fail
mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :artifact_create_fail
echo.
echo [4/6] Forced no-incremental Application.Tests rebuild...
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :focused_build_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION2=1"
echo.
echo [5/6] Focused canonical-v9 baseline equivalence, repaired mode-2 120 s health/ownership and deterministic repeat...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3ShortExactV9EquivalentShadowCompositionRequalification2Tests.R3_RepairedMode2Shadow_PreservesExactV9EquivalentShortHealthOwnershipAndDeterminism" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION2="
for %%F in (02-shadow-baseline-equivalence.csv 03-mode2-shadow-health-trajectory.csv 04-ownership-conservation-summary.txt 05-deterministic-repeat.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION2_ORDINARY_PASS=1"
echo.
echo [6/6] R3-2 evidence integrity and bounded adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification2.ps1"
if errorlevel 1 goto :adjudication_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION2_ORDINARY_PASS="
for %%F in (01-contract-and-provenance.txt 02-shadow-baseline-equivalence.csv 03-mode2-shadow-health-trajectory.csv 04-ownership-conservation-summary.txt 05-deterministic-repeat.txt 06-r3-requalification-summary.txt 07-prequalification-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing
echo.
echo M10 Final VR2 R3 Short Exact-v9-Equivalent Shadow Composition Requalification 2 completed.
echo Return the full "%REPORT_DIR%" folder before R4 planning.
exit /b 0
:artifact_cleanup_fail
echo R3-2 FAILED: stale artifact directory could not be removed.
exit /b 1
:artifact_create_fail
echo R3-2 FAILED: artifact directory could not be created.
exit /b 1
:preordinary_build_fail
echo R3-2 FAILED forced pre-ordinary full solution rebuild.
exit /b 1
:ordinary_fail
echo R3-2 FAILED ordinary Release regression.
exit /b 1
:focused_build_fail
echo R3-2 FAILED focused-project rebuild.
exit /b 1
:focused_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION2="
echo R3-2 FAILED focused shadow-composition evidence.
echo Preserve and return any files under "%REPORT_DIR%".
exit /b 1
:adjudication_fail
set "NRS_M10_FINAL_VR2_R3_REQUALIFICATION2_ORDINARY_PASS="
echo R3-2 evidence adjudication FAILED.
exit /b 1
:missing
echo R3-2 FAILED: required evidence artifact missing.
exit /b 1
:fail
echo R3-2 FAILED before controlled evidence completion.
exit /b 1
