@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1"
set "OPT=NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERF1"
set "RUN=NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_PERF_RUN_INDEX"

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERF1="
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_PERF_RUN_INDEX="

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 FULL-DOMAIN PERFORMANCE CONFIRMATION 1
echo ============================================================
echo Evidence-only confirmation on immutable C4 and frozen RP1A corpora.
echo No RP1C selection, production repair, threshold change, exact-v9 change,
echo VR3, P3-R1 or second replacement-long authorization.
echo.

echo [1/4] Static returned-planning, candidate/corpus pins and confirmation-contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Ordinary Release gate with confirmation opt-in/run variables unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%" >nul 2>nul
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERF1=1"

echo.
echo [3/4] Five fresh full-domain timing processes...
for /L %%R in (1,1,5) do (
  echo.
  echo ---- Confirmation process %%R of 5 ----
  set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_PERF_RUN_INDEX=%%R"
  dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2EngineeringRepairPlanning1Rp1cC4FullDomainPerformanceConfirmation1Tests.Rp1cC4FullDomainPerformanceConfirmation1_MeasuresOneFreshProcess" --parallel none
  if errorlevel 1 goto :focused_fail
)
set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_PERF_RUN_INDEX="

for /L %%R in (1,1,5) do (
  for %%F in (
    01-process-contract.txt
    02-exact-v9-call-timing.csv
    03-seam-call-timing.csv
    04-runtime-context.txt
    05-process-summary.txt
  ) do if not exist "%REPORT_DIR%\process-0%%R\%%F" goto :missing
)

echo.
echo [4/4] Aggregate evidence integrity and full-domain engineering classification...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation1.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (
  01-contract-and-provenance.txt
  02-cross-process-run-summary.csv
  03-cross-process-seam-side-summary.csv
  04-confirmation-evidence-adjudication.txt
  05-rp1c-c4-full-domain-performance-confirmation1-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

set "NRS_M10_FINAL_VR2_REPAIR_PLANNING1_RP1C_C4_FULL_DOMAIN_PERF1="

echo.
echo M10 Final VR2 Engineering Repair Planning 1 RP1C C4 Full-Domain Performance Confirmation 1 completed.
echo Classification is evidence only; this runner does not authorize RP1C selection or production repair.
echo Return the full "%REPORT_DIR%" folder before any RP1C selection planning or production/runtime change.
exit /b 0

:focused_fail
echo.
echo Full-Domain Performance Confirmation 1 FAILED during a focused evidence process.
echo This indicates harness/evidence-integrity failure, not an engineering-negative performance classification.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:adjudication_fail
echo.
echo Full-Domain Performance Confirmation 1 evidence adjudication FAILED integrity checks.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:missing
echo.
echo Full-Domain Performance Confirmation 1 FAILED: required evidence artifact is missing.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:fail
echo.
echo Full-Domain Performance Confirmation 1 FAILED before focused evidence completion.
echo No RP1C selection or production/runtime authority is granted.
exit /b 1
