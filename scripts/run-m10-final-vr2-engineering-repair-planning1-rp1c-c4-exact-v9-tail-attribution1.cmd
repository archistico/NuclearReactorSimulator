@echo off
setlocal
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1"
set "METHOD=NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1cC4ExactV9TailAttribution1Tests.Rp1cC4ExactV9TailAttribution1_MeasuresOneFreshRuntimeModeProcess"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 EXACT-V9 TAIL ATTRIBUTION 1 REV1
echo ============================================================
echo Evidence-only runtime attribution on immutable C4 and frozen exact-v9 corpus.
echo Counterbalanced blocked process order reduces time/order confounding across runtime modes.
echo No automatic causal promotion, RP1C selection, production repair, threshold change,
echo exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
echo.

if defined DOTNET_TieredCompilation goto :dirty_env
if defined DOTNET_TieredPGO goto :dirty_env
if defined DOTNET_TC_QuickJit goto :dirty_env
if defined DOTNET_TC_QuickJitForLoops goto :dirty_env
if defined DOTNET_ReadyToRun goto :dirty_env

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_TAIL_ATTRIBUTION1="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_TAIL_MODE="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_TAIL_RUN_INDEX="

echo [1/4] Static returned-planning, immutable-candidate/corpus and attribution-contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Ordinary Release gate with attribution opt-in unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

if defined DOTNET_TieredCompilation goto :dirty_env
if defined DOTNET_TieredPGO goto :dirty_env
if defined DOTNET_TC_QuickJit goto :dirty_env
if defined DOTNET_TC_QuickJitForLoops goto :dirty_env
if defined DOTNET_ReadyToRun goto :dirty_env

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%" >nul 2>nul
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_TAIL_ATTRIBUTION1=1"

echo.
echo [3/4] Twenty fresh exact-v9 runtime-mode processes in counterbalanced blocked order...

rem Block/run 1: A B C D
call :run_one AMBIENT-UNSET-CONTROL 1 "" "" "" "" ""
if errorlevel 1 goto :focused_fail
call :run_one TIERING-OFF 1 0 0 0 0 1
if errorlevel 1 goto :focused_fail
call :run_one TIERING-ON-PGO-OFF 1 1 0 1 0 1
if errorlevel 1 goto :focused_fail
call :run_one TIERING-ON-PGO-ON 1 1 1 1 0 1
if errorlevel 1 goto :focused_fail

rem Block/run 2: B C D A
call :run_one TIERING-OFF 2 0 0 0 0 1
if errorlevel 1 goto :focused_fail
call :run_one TIERING-ON-PGO-OFF 2 1 0 1 0 1
if errorlevel 1 goto :focused_fail
call :run_one TIERING-ON-PGO-ON 2 1 1 1 0 1
if errorlevel 1 goto :focused_fail
call :run_one AMBIENT-UNSET-CONTROL 2 "" "" "" "" ""
if errorlevel 1 goto :focused_fail

rem Block/run 3: C D A B
call :run_one TIERING-ON-PGO-OFF 3 1 0 1 0 1
if errorlevel 1 goto :focused_fail
call :run_one TIERING-ON-PGO-ON 3 1 1 1 0 1
if errorlevel 1 goto :focused_fail
call :run_one AMBIENT-UNSET-CONTROL 3 "" "" "" "" ""
if errorlevel 1 goto :focused_fail
call :run_one TIERING-OFF 3 0 0 0 0 1
if errorlevel 1 goto :focused_fail

rem Block/run 4: D A B C
call :run_one TIERING-ON-PGO-ON 4 1 1 1 0 1
if errorlevel 1 goto :focused_fail
call :run_one AMBIENT-UNSET-CONTROL 4 "" "" "" "" ""
if errorlevel 1 goto :focused_fail
call :run_one TIERING-OFF 4 0 0 0 0 1
if errorlevel 1 goto :focused_fail
call :run_one TIERING-ON-PGO-OFF 4 1 0 1 0 1
if errorlevel 1 goto :focused_fail

rem Block/run 5: A C B D
call :run_one AMBIENT-UNSET-CONTROL 5 "" "" "" "" ""
if errorlevel 1 goto :focused_fail
call :run_one TIERING-ON-PGO-OFF 5 1 0 1 0 1
if errorlevel 1 goto :focused_fail
call :run_one TIERING-OFF 5 0 0 0 0 1
if errorlevel 1 goto :focused_fail
call :run_one TIERING-ON-PGO-ON 5 1 1 1 0 1
if errorlevel 1 goto :focused_fail

call :verify_mode AMBIENT-UNSET-CONTROL
if errorlevel 1 goto :missing
call :verify_mode TIERING-OFF
if errorlevel 1 goto :missing
call :verify_mode TIERING-ON-PGO-OFF
if errorlevel 1 goto :missing
call :verify_mode TIERING-ON-PGO-ON
if errorlevel 1 goto :missing

set "DOTNET_TieredCompilation="
set "DOTNET_TieredPGO="
set "DOTNET_TC_QuickJit="
set "DOTNET_TC_QuickJitForLoops="
set "DOTNET_ReadyToRun="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_TAIL_RUN_INDEX="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_TAIL_MODE="

echo.
echo [4/4] Aggregate evidence integrity and evidence-only runtime-mode summary...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-exact-v9-tail-attribution1.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (
  01-contract-and-provenance.txt
  02-runtime-mode-run-summary.csv
  03-tail-pass-window-summary.csv
  04-tail-row-path-summary.csv
  05-attribution-evidence-summary.txt
  06-attribution1-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_EXACT_V9_TAIL_ATTRIBUTION1="

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C C4 Exact-v9 Tail Attribution 1 REV1 completed.
echo Classification is evidence only; this runner does not prove runtime causality or authorize RP1C selection.
echo Return the full "%REPORT_DIR%" folder before any causal adjudication, RP1C selection planning or production/runtime change.
exit /b 0

:run_one
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_TAIL_MODE=%~1"
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_TAIL_RUN_INDEX=%~2"
set "DOTNET_TieredCompilation=%~3"
set "DOTNET_TieredPGO=%~4"
set "DOTNET_TC_QuickJit=%~5"
set "DOTNET_TC_QuickJitForLoops=%~6"
set "DOTNET_ReadyToRun=%~7"
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
echo Exact-v9 Tail Attribution 1 REV1 NOT STARTED: CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN.
echo The caller already defines one or more DOTNET compilation variables.
echo The runner will not silently clear caller configuration because the ambient control must be trustworthy.
echo Required unset variables:
echo   DOTNET_TieredCompilation
echo   DOTNET_TieredPGO
echo   DOTNET_TC_QuickJit
echo   DOTNET_TC_QuickJitForLoops
echo   DOTNET_ReadyToRun
echo No RP1C selection or production/runtime authority is granted.
exit /b 1

:focused_fail
echo.
echo Exact-v9 Tail Attribution 1 REV1 FAILED during a focused evidence process.
echo This indicates harness/evidence-integrity failure, not a causal or performance verdict.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:adjudication_fail
echo.
echo Exact-v9 Tail Attribution 1 REV1 evidence adjudication FAILED integrity checks.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:missing
echo.
echo Exact-v9 Tail Attribution 1 REV1 FAILED: required evidence artifact is missing.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:fail
echo.
echo Exact-v9 Tail Attribution 1 REV1 FAILED before focused evidence completion.
echo No causal, RP1C selection or production/runtime authority is granted.
exit /b 1
