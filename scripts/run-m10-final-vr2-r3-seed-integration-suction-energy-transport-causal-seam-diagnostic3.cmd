@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "OUT=%CD%\artifacts\m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3"
if exist "%OUT%" rmdir /s /q "%OUT%"
echo ============================================================
echo M10 FINAL - VR2 R3 SUCTION ENERGY-TRANSPORT CAUSAL-SEAM DIAGNOSTIC 3
echo ============================================================
echo Test-only diagnostic. No production repair, seed retuning, threshold change, R3 PASS or R4 authority.
echo.
echo [1/4] Static contract / returned-evidence audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3.ps1" || goto :fail
echo [2/4] Restore and focused Release build...
dotnet restore || goto :fail
dotnet build ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-restore --no-incremental || goto :fail
echo [3/4] Exact one-step causal-seam evidence generation...
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Tests.SeedStep1_SuctionEnergyBalance_AttributesPhaseFlipToForwardInverseTransportSeam" --parallel none || goto :fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3="
echo [4/4] Evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3.ps1" || goto :fail
for %%F in (01-step1-suction-energy-balance.csv 02-mode2-suction-inverse-path.csv 03-forward-saturation-provider-seam.csv 04-diagnostic-summary.txt 05-pre-repair-review.txt) do if not exist "%OUT%\%%F" goto :fail
echo.
echo M10 Final VR2 R3 Suction Energy-Transport Causal-Seam Diagnostic 3 completed.
echo Return the full "%OUT%" folder before repair planning or any production change.
exit /b 0
:fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3="
echo.
echo Diagnostic 3 FAILED.
exit /b 1
