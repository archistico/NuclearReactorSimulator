@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
echo ============================================================
echo M10 FINAL - VR2 R3 POST-REPAIR CAUSAL-CLOSURE DIAGNOSTIC 1
echo ============================================================
echo Test/evidence only. No production edit, retuning, threshold change or R3 PASS.
echo.
echo [1/4] Static repair-surface and fast-gate evidence audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-energy-transport-ownership-repair-implementation1-post-repair-causal-closure-diagnostic1.ps1" || goto :fail
echo [2/4] Fresh Release build of Application.Tests...
dotnet clean ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release >nul || goto :fail
dotnet restore || goto :fail
dotnet build ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-restore --no-incremental --warnaserror || goto :fail
echo [3/4] Re-run the exact Diagnostic 3 REV1 production seed-step1 capture against repaired production...
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1Tests.SeedStep1_SuctionEnergyTransport_EmitsRuntimeEvidenceForIndependentCounterfactual" --parallel none || goto :fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1="
echo [4/4] Adjudicate repaired causal seam and historical fast-gate contradiction...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-energy-transport-ownership-repair-implementation1-post-repair-causal-closure-diagnostic1.ps1" || goto :fail
echo.
echo R3 Post-Repair Causal-Closure Diagnostic 1 completed.
echo Return the complete artifacts\r3-post-repair-closure-d1 folder.
exit /b 0
:fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1="
echo.
echo R3 Post-Repair Causal-Closure Diagnostic 1 FAILED.
exit /b 1
