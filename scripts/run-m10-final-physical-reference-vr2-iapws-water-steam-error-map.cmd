@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2"
set "NRS_M10_FINAL_PHYSICAL_REFERENCE_VR2=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

 echo ============================================================
 echo M10 FINAL - VR2 IAPWS-IF97 WATER/STEAM ERROR MAP
 echo ============================================================
 echo Explicit external thermodynamic model-assessment gate.
 echo Independent test-only IAPWS Regions 1/2/4 reference helper.
 echo No production repair, exact-v9 change, P3-R1,
 echo workload/protection change or second-long authorization.
 echo.

 echo [1/3] Static VR2 contract and VR1 prerequisite audit...
 powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-physical-reference-vr2.ps1"
 if errorlevel 1 goto :fail

 echo.
 echo [2/3] Ordinary Release gate identical to CI entry point...
 call eng\ci-ordinary.cmd
 if errorlevel 1 goto :fail

 echo.
 echo [3/3] Focused explicit VR2 IAPWS-IF97 water/steam model assessment...
 dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalPhysicalReferenceVr2WaterSteamBenchmarkTests.SimplifiedWaterSteamModel_IsMappedAgainstIndependentIapwsIf97Reference" --parallel none
 if errorlevel 1 goto :assessment_fail

 for %%F in (
   01-source-provenance-manifest.txt
   02-frozen-point-matrix.csv
   03-reference-selfcheck.csv
   04-numerical-error-map.csv
   05-inverse-state-comparison.csv
   06-property-domain-summary.csv
   07-vr2-assessment-summary.txt
   08-impact-known-limitations.txt
   09-deterministic-repeat.txt
 ) do if not exist "%REPORT_DIR%\%%F" goto :missing

 echo.
 echo M10 Final VR2 IAPWS-IF97 Water/Steam Error Map completed.
 echo Return the full "%REPORT_DIR%" folder before VR3, changing thermodynamic physics/tolerances, executing P3-R1, or authorizing a second replacement-long baseline.
 exit /b 0

:assessment_fail
 echo.
 echo M10 Final VR2 assessment did not meet its frozen gate contract.
 echo Preserve and return the complete "%REPORT_DIR%" folder before any repair or tolerance change.
 exit /b 1

:missing
 echo.
 echo M10 Final VR2 FAILED: expected artifacts are missing.
 echo Preserve and return any files already written under "%REPORT_DIR%".
 exit /b 1

:fail
 echo.
 echo M10 Final VR2 FAILED before focused assessment completion.
 echo Preserve and return any files already written under "%REPORT_DIR%".
 exit /b 1
