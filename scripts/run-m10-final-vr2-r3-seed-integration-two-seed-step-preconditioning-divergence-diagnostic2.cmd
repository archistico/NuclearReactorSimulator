@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "OUT=%CD%\artifacts\m10-final-physical-reference-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2"
if exist "%OUT%" rmdir /s /q "%OUT%"
echo ============================================================
echo M10 FINAL - VR2 R3 SEED INTEGRATION TWO-SEED-STEP PRECONDITIONING DIVERGENCE DIAGNOSTIC 2
echo ============================================================
echo Test-only localization. No seed retuning, production repair, threshold change, R3 PASS or R4 authority.
echo.
echo [1/4] Static predecessor, frozen-evidence and immutable-baseline audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2.ps1" || goto :fail
echo [2/4] Restore and forced no-incremental Application.Tests build...
dotnet restore || goto :fail
dotnet build ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-restore --no-incremental || goto :fail
echo [3/4] Focused raw -^> seed step 1 -^> seed step 2 localization diagnostic...
set "NRS_M10_FINAL_VR2_R3_SEED_PRECONDITIONING_DIAGNOSTIC2=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3SeedIntegrationTwoSeedStepPreconditioningDivergenceDiagnostic2Tests.PreconditioningStep1AndStep2_LocalizeFirstMode1Mode2Displacement" --parallel none || goto :fail
set "NRS_M10_FINAL_VR2_R3_SEED_PRECONDITIONING_DIAGNOSTIC2="
echo [4/4] Evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-seed-integration-two-seed-step-preconditioning-divergence-diagnostic2.ps1" || goto :fail
for %%F in (01-raw-checkpoint-node-comparison.csv 02-seed-step1-node-comparison.csv 03-seed-step2-node-comparison.csv 04-seed-step-hydraulic-heads.csv 05-seed-step-flow-comparison.csv 06-seed-step-controller-turbine.csv 07-diagnostic-summary.txt 08-pre-repair-review.txt) do if not exist "%OUT%\%%F" goto :fail
echo.
echo R3 Seed Integration Two-Seed-Step Preconditioning Divergence Diagnostic 2 completed.
echo Return the full "%OUT%" folder before any repair planning or implementation.
exit /b 0
:fail
set "NRS_M10_FINAL_VR2_R3_SEED_PRECONDITIONING_DIAGNOSTIC2="
echo Diagnostic 2 FAILED.
exit /b 1
