@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-reference-consistent-seed-integration-implementation1"
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1="
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1_ORDINARY_PASS="
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :cleanup_fail
echo ============================================================
echo M10 FINAL - VR2 R3 REFERENCE-CONSISTENT SEED INTEGRATION IMPLEMENTATION 1
echo ============================================================
echo Three-file bounded opt-in seed integration. R3 remains RED until Requalification 3.
echo.
echo [1/6] Static returned-planning, bounded-source, canonical-v9 and frozen-vector audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-reference-consistent-seed-integration-implementation1.ps1"
if errorlevel 1 goto :fail
echo.
echo [2/6] Restore and forced full no-incremental Release build...
dotnet restore
if errorlevel 1 goto :build_fail
dotnet build --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :build_fail
echo.
echo [3/6] Ordinary Release suite with implementation opt-in unset...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :ordinary_fail
mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :create_fail
> "%REPORT_DIR%\01-contract-and-provenance.txt" (
echo status=IMPLEMENTATION-EVIDENCE-WRITTEN
echo implementation-id=R3-REFERENCE-CONSISTENT-SEED-INTEGRATION-IMPLEMENTATION1
echo predecessor=R3-REFERENCE-CONSISTENT-SEED-INTEGRATION-PLANNING1-PASS-AS-AUTHORED
echo production-files-changed=3
echo frozen-candidate-nodes=12
echo canonical-exact-v9-method-body=UNCHANGED
echo ordinary-release-suite=PASS
)
> "%REPORT_DIR%\02-production-change-manifest.csv" (
echo area,path
echo production,src/NuclearReactorSimulator.Application/Scenarios/PreStartup/OperationalFluidNodeSeed.cs
echo production,src/NuclearReactorSimulator.Application/Scenarios/PreStartup/ColdShutdownInitialConditionFactory.cs
echo production,src/NuclearReactorSimulator.Application/Scenarios/Training/DesktopSustainedGenerationInitialConditionFactory.cs
echo test,tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/M10FinalVr2R3ReferenceConsistentSeedIntegrationImplementation1Tests.cs
)
echo.
echo [4/6] Forced no-incremental Application.Tests rebuild...
dotnet build "%PROJECT%" --configuration Release --no-restore --no-incremental
if errorlevel 1 goto :focused_build_fail
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1=1"
echo.
echo [5/6] Focused raw-vector, two-seed-step and first-100-step exact-v9 health gate...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3ReferenceConsistentSeedIntegrationImplementation1Tests.OptInReferenceConsistentSeed_PreservesRawTargetsAndFirstHundredStepExactV9Health" --parallel none
if errorlevel 1 goto :focused_fail
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1="
for %%F in (03-raw-seed-roundtrip.csv 04-fast-100-step-health.csv) do if not exist "%REPORT_DIR%\%%F" goto :missing
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1_ORDINARY_PASS=1"
echo.
echo [6/6] Post-run bounded-source audit and implementation evidence adjudication...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-reference-consistent-seed-integration-implementation1.ps1"
if errorlevel 1 goto :postrun_static_fail
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-r3-reference-consistent-seed-integration-implementation1.ps1"
if errorlevel 1 goto :postrun_static_fail
echo Implementation 1 FAILED post-run bounded-source audit.
exit /b 1
:adjudication_fail
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1_ORDINARY_PASS="
for %%F in (01-contract-and-provenance.txt 02-production-change-manifest.csv 03-raw-seed-roundtrip.csv 04-fast-100-step-health.csv 05-implementation-summary.txt 06-pre-requalification-review.txt) do if not exist "%REPORT_DIR%\%%F" goto :missing
echo.
echo R3 Reference-Consistent Seed Integration Implementation 1 completed.
echo Classification is implementation evidence only; R3 remains RED until Requalification 3.
echo Return the full "%REPORT_DIR%" folder.
exit /b 0
:cleanup_fail
echo Implementation 1 FAILED: stale artifact directory could not be removed.
exit /b 1
:create_fail
echo Implementation 1 FAILED: artifact directory could not be created.
exit /b 1
:build_fail
echo Implementation 1 FAILED forced full Release build.
exit /b 1
:ordinary_fail
echo Implementation 1 FAILED ordinary Release regression.
exit /b 1
:focused_build_fail
echo Implementation 1 FAILED focused Application.Tests build.
exit /b 1
:focused_fail
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1="
echo Implementation 1 FAILED focused fast gate.
echo Preserve and return any files under "%REPORT_DIR%".
exit /b 1
:adjudication_fail
set "NRS_M10_FINAL_VR2_R3_SEED_INTEGRATION_IMPLEMENTATION1_ORDINARY_PASS="
echo Implementation 1 evidence adjudication FAILED.
exit /b 1
:missing
echo Implementation 1 FAILED: required artifact missing.
exit /b 1
:fail
echo Implementation 1 FAILED before controlled evidence completion.
exit /b 1
