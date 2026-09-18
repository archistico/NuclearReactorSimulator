@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 RUNTIME CONFIGURATION IMPACT ASSESSMENT 1
echo ============================================================
echo Evidence-only Branch A2 assessment of AMBIENT-UNSET versus EXPLICIT-REFERENCE-ALL-ON.
echo One complete gate must run on one physical host with a stable power scheme.
echo Engineering-negative outcomes are retained as evidence and are not converted into infrastructure RED.
echo No FDPC2, RP1C selection, production/runtime change, threshold change, exact-v9 change, VR3,
echo P3-R1 or second replacement-long authorization.
echo.

if defined DOTNET_TieredCompilation goto :dirty_env
if defined DOTNET_TieredPGO goto :dirty_env
if defined DOTNET_TC_QuickJit goto :dirty_env
if defined DOTNET_TC_QuickJitForLoops goto :dirty_env
if defined DOTNET_ReadyToRun goto :dirty_env
if defined COMPlus_TieredCompilation goto :dirty_env
if defined COMPlus_TieredPGO goto :dirty_env
if defined COMPlus_TC_QuickJit goto :dirty_env
if defined COMPlus_TC_QuickJitForLoops goto :dirty_env
if defined COMPlus_ReadyToRun goto :dirty_env

echo [1/4] Static returned-evidence, host-provenance, compact-store and A2 contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Restore and Release build before controlled profile execution...
dotnet restore
if errorlevel 1 goto :build_fail
dotnet build --configuration Release --no-restore
if errorlevel 1 goto :build_fail

echo.
echo [3/4] Same-host A2 evidence collection: exact-v9, ordinary suite, replay/determinism and non-VR2 hot-path...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\invoke-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 goto :evidence_fail

echo.
echo [4/4] Evidence integrity, complete 64 x 360 matrices, host consistency and project-impact summaries...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment1.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (
  01-contract-and-provenance.txt
  02-execution-host-provenance.txt
  03-host-consistency-audit.txt
  04-exact-v9-profile-summary.csv
  05-ordinary-suite-impact-summary.csv
  06-replay-determinism-impact-summary.csv
  07-non-vr2-performance-impact-summary.csv
  08-runtime-configuration-impact-assessment1-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C C4 Runtime Configuration Impact Assessment 1 completed.
echo Classification is evidence only; this runner does not auto-promote ambient/default equivalence or recommend a production runtime configuration.
echo Return the full "%REPORT_DIR%" folder before FDPC2 planning, RP1C selection or any production/runtime change.
exit /b 0

:dirty_env
echo.
echo Runtime Configuration Impact Assessment 1 NOT STARTED: CALLER-RUNTIME-CONFIGURATION-NOT-CLEAN.
echo The caller already defines one or more DOTNET compilation variables.
echo Required unset variables:
echo   DOTNET_TieredCompilation
echo   DOTNET_TieredPGO
echo   DOTNET_TC_QuickJit
echo   DOTNET_TC_QuickJitForLoops
echo   DOTNET_ReadyToRun
echo and the equivalent COMPlus_* aliases must also be unset.
echo No A2, FDPC2, RP1C selection or production/runtime authority is granted.
exit /b 1

:build_fail
echo.
echo Runtime Configuration Impact Assessment 1 FAILED before evidence collection during restore/build.
echo No A2 engineering conclusion or downstream authority is granted.
exit /b 1

:evidence_fail
echo.
echo Runtime Configuration Impact Assessment 1 FAILED infrastructure/evidence collection controls.
echo Preserve and return any files already written under "%REPORT_DIR%".
echo This is not automatically an engineering verdict against either runtime profile.
exit /b 1

:adjudication_fail
echo.
echo Runtime Configuration Impact Assessment 1 evidence integrity adjudication FAILED.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:missing
echo.
echo Runtime Configuration Impact Assessment 1 FAILED: required aggregate evidence artifact is missing.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:fail
echo.
echo Runtime Configuration Impact Assessment 1 FAILED before controlled profile evidence completion.
echo No FDPC2, RP1C selection or production/runtime authority is granted.
exit /b 1
