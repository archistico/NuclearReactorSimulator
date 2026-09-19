@echo off
setlocal
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator1"
set "METHOD=NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1cC4DynamicPgoComparator1Tests.Rp1cC4DynamicPgoComparator1_MeasuresOneFreshPgoModeProcess"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 EXACT-V9 DYNAMIC PGO COMPARATOR 1
echo ============================================================
echo Evidence-only QJFL-on Dynamic PGO OFF/ON comparator on immutable C4 and frozen exact-v9 corpus.
echo TieredCompilation, QuickJit, QuickJitForLoops and ReadyToRun remain fixed ON; only DOTNET_TieredPGO changes.
echo No automatic causal promotion, Runtime Configuration Impact Assessment, RP1C selection, production runtime change,
echo production repair, threshold change, exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
echo.

if defined DOTNET_TieredCompilation goto :dirty_env
if defined DOTNET_TieredPGO goto :dirty_env
if defined DOTNET_TC_QuickJit goto :dirty_env
if defined DOTNET_TC_QuickJitForLoops goto :dirty_env
if defined DOTNET_ReadyToRun goto :dirty_env

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR1="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_MODE="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_RUN_INDEX="

echo [1/4] Static returned-planning, immutable-candidate/corpus and Dynamic PGO Comparator contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Ordinary Release gate with Dynamic PGO Comparator opt-in unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

if defined DOTNET_TieredCompilation goto :dirty_env
if defined DOTNET_TieredPGO goto :dirty_env
if defined DOTNET_TC_QuickJit goto :dirty_env
if defined DOTNET_TC_QuickJitForLoops goto :dirty_env
if defined DOTNET_ReadyToRun goto :dirty_env

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%" >nul 2>nul
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR1=1"

echo.
echo [3/4] Ten fresh exact-v9 Dynamic PGO processes in counterbalanced blocked order...

rem Block 1: OFF ON
call :run_one PGO-OFF-QJFL-ON 1 0
if errorlevel 1 goto :focused_fail
call :run_one PGO-ON-QJFL-ON 1 1
if errorlevel 1 goto :focused_fail

rem Block 2: ON OFF
call :run_one PGO-ON-QJFL-ON 2 1
if errorlevel 1 goto :focused_fail
call :run_one PGO-OFF-QJFL-ON 2 0
if errorlevel 1 goto :focused_fail

rem Block 3: OFF ON
call :run_one PGO-OFF-QJFL-ON 3 0
if errorlevel 1 goto :focused_fail
call :run_one PGO-ON-QJFL-ON 3 1
if errorlevel 1 goto :focused_fail

rem Block 4: ON OFF
call :run_one PGO-ON-QJFL-ON 4 1
if errorlevel 1 goto :focused_fail
call :run_one PGO-OFF-QJFL-ON 4 0
if errorlevel 1 goto :focused_fail

rem Block 5: OFF ON
call :run_one PGO-OFF-QJFL-ON 5 0
if errorlevel 1 goto :focused_fail
call :run_one PGO-ON-QJFL-ON 5 1
if errorlevel 1 goto :focused_fail

call :verify_mode PGO-OFF-QJFL-ON
if errorlevel 1 goto :missing
call :verify_mode PGO-ON-QJFL-ON
if errorlevel 1 goto :missing

set "DOTNET_TieredCompilation="
set "DOTNET_TieredPGO="
set "DOTNET_TC_QuickJit="
set "DOTNET_TC_QuickJitForLoops="
set "DOTNET_ReadyToRun="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_RUN_INDEX="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_MODE="

echo.
echo [4/4] Aggregate evidence integrity, per-mode summary and Dynamic PGO single-factor contrast...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-dynamic-pgo-comparator1.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (
  01-contract-and-provenance.txt
  02-pgo-run-summary.csv
  03-pgo-contrast-summary.csv
  04-tail-row-path-summary.csv
  05-pgo-evidence-summary.txt
  06-dynamic-pgo-comparator1-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_COMPARATOR1="

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C C4 Exact-v9 Dynamic PGO Comparator 1 completed.
echo Classification is evidence only; this runner does not auto-promote a Dynamic PGO causal claim or authorize Branch A2/RP1C selection.
echo Return the full "%REPORT_DIR%" folder before any PGO causal adjudication, Runtime Configuration Impact Assessment planning or production/runtime change.
exit /b 0

:run_one
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_MODE=%~1"
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_DYNAMIC_PGO_RUN_INDEX=%~2"
set "DOTNET_TieredCompilation=1"
set "DOTNET_TieredPGO=%~3"
set "DOTNET_TC_QuickJit=1"
set "DOTNET_TC_QuickJitForLoops=1"
set "DOTNET_ReadyToRun=1"
echo.
echo ---- %~1 process %~2 of 5 ----
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "%METHOD%" --parallel none
exit /b %ERRORLEVEL%

:verify_mode
for /L %%R in (1,1,5) do (
  for %%F in (
    01-process-contract.txt
    02-exact-v9-call-timing.csv
    03-runtime-context.txt
    04-process-summary.txt
  ) do if not exist "%REPORT_DIR%\mode-%~1\process-0%%R\%%F" exit /b 1
)
exit /b 0

:dirty_env
echo.
echo Exact-v9 Dynamic PGO Comparator 1 NOT STARTED: CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN.
echo The caller already defines one or more DOTNET compilation variables.
echo The runner will not silently clear caller configuration because the single-factor child environments must start from a clean caller.
echo Required unset variables:
echo   DOTNET_TieredCompilation
echo   DOTNET_TieredPGO
echo   DOTNET_TC_QuickJit
echo   DOTNET_TC_QuickJitForLoops
echo   DOTNET_ReadyToRun
echo No Runtime Configuration Impact Assessment, RP1C selection or production/runtime authority is granted.
exit /b 1

:focused_fail
echo.
echo Exact-v9 Dynamic PGO Comparator 1 FAILED during a focused evidence process.
echo This indicates harness/evidence-integrity failure, not a causal or performance verdict.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:adjudication_fail
echo.
echo Exact-v9 Dynamic PGO Comparator 1 evidence adjudication FAILED integrity checks.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:missing
echo.
echo Exact-v9 Dynamic PGO Comparator 1 FAILED: required evidence artifact is missing.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:fail
echo.
echo Exact-v9 Dynamic PGO Comparator 1 FAILED before focused evidence completion.
echo No PGO causal, Runtime Configuration Impact Assessment, RP1C selection or production/runtime authority is granted.
exit /b 1
