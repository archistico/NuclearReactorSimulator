@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "OUT=%CD%\artifacts\m10-final-physical-reference-vr2-r3-reference-consistent-seed-integration-fast-gate-dynamic-equilibrium-diagnostic1"
if exist "%OUT%" rmdir /s /q "%OUT%"
echo ============================================================
echo M10 FINAL - VR2 R3 SEED INTEGRATION FAST-GATE DYNAMIC EQUILIBRIUM DIAGNOSTIC 1
echo ============================================================
echo Test-only diagnostic. No production repair, threshold change, R3 PASS or R4 authority.
echo.
echo [1/4] Static failed-fast evidence and frozen-baseline audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-seed-fast-dynamic-equilibrium-diagnostic1.ps1" || goto :fail
echo [2/4] Restore and forced no-incremental Application.Tests build...
dotnet restore || goto :fail
dotnet build ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-restore --no-incremental || goto :fail
echo [3/4] Focused canonical-v9 vs reference-consistent first-100-step diagnostic...
set "NRS_M10_FINAL_VR2_R3_SEED_FAST_DYNAMIC_DIAGNOSTIC1=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3ReferenceConsistentSeedIntegrationFastGateDynamicEquilibriumDiagnostic1Tests.ReturnedFastGateRed_LocalizesPostSeedDynamicEquilibriumDivergence" --parallel none || goto :fail
set "NRS_M10_FINAL_VR2_R3_SEED_FAST_DYNAMIC_DIAGNOSTIC1="
echo [4/4] Evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-seed-fast-dynamic-equilibrium-diagnostic1.ps1" || goto :fail
for %%F in (01-post-seed-node-comparison.csv 02-first-100-step-key-dynamics.csv 03-first-100-step-hydraulic-heads.csv 04-first-100-step-controller-turbine.csv 05-diagnostic-summary.txt 06-pre-repair-review.txt) do if not exist "%OUT%\%%F" goto :fail
echo.
echo R3 Seed Integration Fast-Gate Dynamic Equilibrium Diagnostic 1 completed.
echo Return the full "%OUT%" folder before any production repair or Requalification 3.
exit /b 0
:fail
set "NRS_M10_FINAL_VR2_R3_SEED_FAST_DYNAMIC_DIAGNOSTIC1="
echo Diagnostic 1 FAILED.
exit /b 1
