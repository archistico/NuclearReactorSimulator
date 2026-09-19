@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-mode2-branch-continuity-fusion-repair-implementation1"
set "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1="
set "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1_ORDINARY_PASS="
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :cleanup_fail

echo ============================================================
echo M10 FINAL - VR2 R3 MODE2 BRANCH-CONTINUITY FUSION REPAIR IMPLEMENTATION 1
echo ============================================================
echo Two-file bounded repair. R3 remains RED until the subsequent 120 s requalification passes.
echo.
echo [1/5] Static returned-planning, bounded-source and legacy-projection audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-mode2-branch-continuity-fusion-repair-implementation1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/6] Restoring and forcing a full no-incremental Release build before the ordinary suite...
dotnet restore
if errorlevel 1 goto :preordinary_build_fail
dotnet build --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :preordinary_build_fail

echo.
echo [3/6] Ordinary Release suite with repair opt-in unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :ordinary_fail

mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :create_fail

echo.
echo [4/6] Forced no-incremental Simulation.Tests rebuild...
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :build_fail

set "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1=1"
echo.
echo [5/6] Focused returned-state repair and historical-fusion regression...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2R3Mode2BranchContinuityFusionRepairImplementation1Tests.Repair_PreservesMode0Mode1Fusion_AndRoutesMode2ThroughNonFusedProductionResolve" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1="
for %%F in (03-returned-state-repair-regression.txt 04-historical-fusion-regression.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

set "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1_ORDINARY_PASS=1"
echo.
echo [6/6] Implementation evidence integrity and bounded adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-mode2-branch-continuity-fusion-repair-implementation1.ps1"
if errorlevel 1 goto :adjudication_fail
set "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1_ORDINARY_PASS="
for %%F in (01-contract-and-provenance.txt 02-production-change-manifest.csv 03-returned-state-repair-regression.txt 04-historical-fusion-regression.txt 05-implementation-summary.txt 06-pre-requalification-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo R3 Mode-2 Branch-Continuity Fusion Repair Implementation 1 completed.
echo Classification is implementation evidence only; R3 remains RED until Short Requalification 2.
echo Return the full "%REPORT_DIR%" folder before R3 Short Requalification 2.
exit /b 0
:cleanup_fail
echo Repair Implementation 1 FAILED: stale artifact directory could not be removed.
exit /b 1
:create_fail
echo Repair Implementation 1 FAILED: artifact directory could not be created.
exit /b 1
:preordinary_build_fail
echo Repair Implementation 1 FAILED forced pre-ordinary full solution rebuild.
exit /b 1
:ordinary_fail
echo Repair Implementation 1 FAILED ordinary Release regression.
exit /b 1
:build_fail
echo Repair Implementation 1 FAILED forced focused-project rebuild.
exit /b 1
:focused_fail
set "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1="
echo Repair Implementation 1 FAILED focused repair regression. Preserve "%REPORT_DIR%".
exit /b 1
:adjudication_fail
set "NRS_M10_FINAL_VR2_R3_FUSION_REPAIR_IMPLEMENTATION1_ORDINARY_PASS="
echo Repair Implementation 1 evidence adjudication FAILED. Preserve "%REPORT_DIR%".
exit /b 1
:missing
echo Repair Implementation 1 FAILED: required evidence artifact missing.
exit /b 1
:fail
echo Repair Implementation 1 FAILED before controlled evidence completion.
exit /b 1
