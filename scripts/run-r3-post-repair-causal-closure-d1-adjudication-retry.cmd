@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
echo ============================================================
echo R3 POST-REPAIR CAUSAL-CLOSURE D1 - ADJUDICATION RETRY HOTFIX 4
echo ============================================================
echo Reuses evidence when present; regenerates only the single Diagnostic 3 REV1 test when missing.
echo No build, production, threshold, seed or R3 authority change.
echo.
echo [1/3] Static evidence and repair-surface audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-energy-transport-ownership-repair-implementation1-post-repair-causal-closure-diagnostic1.ps1" || goto :fail
set "EVIDENCE=.\artifacts\m10-final-physical-reference-vr2-r3-seed-integration-suction-energy-transport-causal-seam-diagnostic3-rev1\01-step1-suction-energy-balance.csv"
if exist "%EVIDENCE%" goto :haveEvidence
echo [2/3] Runtime evidence missing; regenerate only the exact Diagnostic 3 REV1 test with existing Release build...
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3SeedIntegrationSuctionEnergyTransportCausalSeamDiagnostic3Rev1Tests.SeedStep1_SuctionEnergyTransport_EmitsRuntimeEvidenceForIndependentCounterfactual" --parallel none || goto :fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1="
if not exist "%EVIDENCE%" (
  echo Runtime evidence is still missing after the focused test.
  goto :fail
)
goto :adjudicate
:haveEvidence
echo [2/3] Existing post-repair Diagnostic 3 REV1 evidence found; no test rerun needed.
:adjudicate
echo [3/3] Adjudicate repaired causal seam in-memory; summary persistence is best-effort...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-energy-transport-ownership-repair-implementation1-post-repair-causal-closure-diagnostic1.ps1" || goto :fail
echo.
echo R3 Post-Repair Causal-Closure Diagnostic 1 adjudication retry completed.
echo Console verdict is authoritative; optional summary: artifacts\r3d1.txt.
exit /b 0
:fail
set "NRS_M10_FINAL_VR2_R3_SUCTION_ENERGY_TRANSPORT_DIAGNOSTIC3_REV1="
echo.
echo R3 Post-Repair Causal-Closure Diagnostic 1 adjudication retry FAILED.
exit /b 1
