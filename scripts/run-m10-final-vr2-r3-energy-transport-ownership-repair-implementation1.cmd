@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

set "ARTIFACT_DIR=%CD%\artifacts\m10-final-vr2-r3-energy-transport-ownership-repair-implementation1"
if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"
mkdir "%ARTIFACT_DIR%" || goto :fail

echo ============================================================
echo M10 FINAL - VR2 R3 ENERGY-TRANSPORT OWNERSHIP REPAIR IMPLEMENTATION 1
echo ============================================================
echo Family B bounded production repair and local fast gate.
echo No retuning, threshold, C4 payload, default-mode or R3 PASS change.
echo.

echo [1/5] Static production-surface and frozen-boundary validation...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-energy-transport-ownership-repair-implementation1.ps1" -Root "%CD%" -ArtifactDirectory "%ARTIFACT_DIR%"
if errorlevel 1 goto :fail

echo.
echo [2/5] Clean + Release build...
dotnet clean ".\NuclearReactorSimulator.sln" --configuration Release --nologo
if errorlevel 1 goto :fail
dotnet restore
if errorlevel 1 goto :fail
dotnet build --configuration Release --no-restore --no-incremental --warnaserror
if errorlevel 1 goto :fail

echo.
echo [3/5] Focused closure-owned transport-property tests...
dotnet test --project ".\tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" --configuration Release --no-build -- --filter-class "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2R3EnergyTransportOwnershipRepairImplementation1Tests" --parallel none
if errorlevel 1 goto :fail

echo.
echo [4/5] Existing 100-step reference-consistent seed-integration fast gate...
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3ReferenceConsistentSeedIntegrationImplementation1Tests.OptInReferenceConsistentSeed_PreservesRawTargetsAndFirstHundredStepExactV9Health" --parallel none
set "FAST_RC=%ERRORLEVEL%"
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1="
if not "%FAST_RC%"=="0" goto :fail

if exist ".\artifacts\m10-final-physical-reference-vr2-r3-reference-consistent-seed-integration-implementation1\04-fast-100-step-health.csv" copy /y ".\artifacts\m10-final-physical-reference-vr2-r3-reference-consistent-seed-integration-implementation1\04-fast-100-step-health.csv" "%ARTIFACT_DIR%\02-fast-100-step-health.csv" >nul

echo.
echo [5/5] Complete ordinary local suite + current frozen-evidence contracts...
call ".\eng\ci-ordinary.cmd"
if errorlevel 1 goto :fail

(
  echo status=PASS-AS-AUTHORED
  echo implementation-id=R3-ENERGY-TRANSPORT-OWNERSHIP-REPAIR-IMPLEMENTATION1
  echo selected-family=B
  echo repair-owner=ACTIVE-CLOSURE-TRANSPORT-PROPERTY-CONTRACT
  echo focused-transport=PASS
  echo fast-100-step=PASS
  echo ordinary-local=PASS
  echo exact-v9-contract-v2=PASS-VIA-CURRENT-EVIDENCE
  echo c4-payload-mutated=False
  echo retuning=False
  echo r3=RED
  echo next-authorized-gate=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION3
) > "%ARTIFACT_DIR%\00-status.txt"

(
  echo status=PASS-PRE-REQUALIFICATION
  echo production-surface-count=5
  echo new-production-path-count=1
  echo historical-modes-bit-identical=True
  echo mode2-lookup=PRESSURE-KEYED-DENSE-SATURATION-LINEAR-INTERPOLATION
  echo runtime-if97=False
  echo payload-change=False
  echo steady-state-lookup-allocation-max-bytes=0
  echo first-100-step-envelope-violations=0
  echo no-retuning-on-red=True
  echo r3-passed=False
) > "%ARTIFACT_DIR%\03-implementation-summary.txt"

echo.
echo R3 Energy-Transport Ownership Repair Implementation 1 completed locally.
echo Fast gate and ordinary regression are GREEN; R3 remains RED.
echo Return the complete folder:
echo %ARTIFACT_DIR%
exit /b 0

:fail
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1="
echo.
echo R3 Energy-Transport Ownership Repair Implementation 1 FAILED.
echo Stop here. Do not retune or start Requalification 3.
exit /b 1
