@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "OUT=%CD%\artifacts\m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1"
if exist "%OUT%" rmdir /s /q "%OUT%"
echo ============================================================
echo M10 FINAL - VR2 R3 SUCTION ENERGY-TRANSPORT CAUSAL-SEAM DIAGNOSTIC 3 REV1
echo ============================================================
echo Test-only diagnostic. No production repair, repair planning, seed retuning, threshold change, R3 PASS or R4 authority.
echo.
echo [1/5] Static contract / provenance / implementation audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1.ps1" || goto :fail
echo [2/5] Restore and focused Release builds...
dotnet restore || goto :fail
dotnet build ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-restore --no-incremental || goto :fail
dotnet build ".\tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" --configuration Release --no-restore --no-incremental || goto :fail
echo [3/5] Production runtime evidence capture...
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1Tests.SeedStep1_SuctionEnergyTransport_EmitsRuntimeEvidenceForIndependentCounterfactual" --parallel none || goto :fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1="
echo [4/5] Independent IAPWS-IF97 reference counterfactual...
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1_REFERENCE=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1ReferenceCounterfactualTests.RawDrumPoint_IndependentIf97Reference_EmitsTransportCounterfactual" --parallel none || goto :fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1_REFERENCE="
echo [5/5] Evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1.ps1" || goto :fail
for %%F in (01-step1-suction-energy-balance.csv 02-mode2-suction-inverse-path.csv 03-forward-transport-decomposition.csv 04-reference-counterfactual-input.csv 05-if97-reference-counterfactual.csv 06-diagnostic-summary.txt 07-pre-repair-review.txt) do if not exist "%OUT%\%%F" goto :fail
echo.
echo M10 Final VR2 R3 Suction Energy-Transport Causal-Seam Diagnostic 3 REV1 completed.
echo Return the full "%OUT%" folder for independent adjudication.
exit /b 0
:fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1="
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1_REFERENCE="
echo.
echo Diagnostic 3 REV1 FAILED.
exit /b 1
