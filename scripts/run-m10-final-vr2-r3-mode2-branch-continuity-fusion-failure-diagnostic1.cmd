@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-mode2-branch-continuity-fusion-failure-diagnostic1"
set "NRS_M10_FINAL_VR2_R3_FUSION_DIAGNOSTIC1="

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :artifact_cleanup_fail

echo ============================================================
echo M10 FINAL - VR2 R3 MODE2 BRANCH-CONTINUITY FUSION FAILURE DIAGNOSTIC 1
echo ============================================================
echo Diagnostic only on returned R3 blocking state. No production repair or R4 authority.
echo.

echo [1/4] Static returned-failure, source and diagnostic-scope audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-mode2-branch-continuity-fusion-failure-diagnostic1.ps1"
if errorlevel 1 goto :fail

mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :artifact_create_fail

echo.
echo [2/4] Forced no-incremental Simulation.Tests rebuild...
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :build_fail

set "NRS_M10_FINAL_VR2_R3_FUSION_DIAGNOSTIC1=1"
echo.
echo [3/4] Focused returned-state direct/fused/non-fused diagnostic...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2R3Mode2BranchContinuityFusionFailureDiagnostic1Tests.FailureState_IsResolvedByMode2Directly_ButRejectedBySameInstanceFusedContinuityPath" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R3_FUSION_DIAGNOSTIC1="

for %%F in (01-failure-state-resolution-matrix.txt 02-fusion-mechanism-summary.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo [4/4] Diagnostic evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-mode2-branch-continuity-fusion-failure-diagnostic1.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (01-failure-state-resolution-matrix.txt 02-fusion-mechanism-summary.txt 03-diagnostic-summary.txt 04-pre-repair-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo R3 mode-2 branch-continuity fusion failure Diagnostic 1 completed.
echo Classification is diagnostic only; R3 remains blocking and production repair is not yet authorized.
echo Return the full "%REPORT_DIR%" folder before repair planning.
exit /b 0

:artifact_cleanup_fail
echo.
echo Diagnostic FAILED: stale artifact directory could not be removed.
exit /b 1

:artifact_create_fail
echo.
echo Diagnostic FAILED: artifact directory could not be created.
exit /b 1

:build_fail
echo.
echo Diagnostic FAILED forced Simulation.Tests rebuild.
exit /b 1

:focused_fail
set "NRS_M10_FINAL_VR2_R3_FUSION_DIAGNOSTIC1="
echo.
echo Diagnostic focused test FAILED.
echo Preserve and return any files under "%REPORT_DIR%".
exit /b 1

:adjudication_fail
echo.
echo Diagnostic evidence adjudication FAILED.
exit /b 1

:missing
echo.
echo Diagnostic FAILED: required artifact missing.
exit /b 1

:fail
echo.
echo Diagnostic FAILED before controlled evidence completion.
exit /b 1
