@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr1"
set "NRS_M10_FINAL_PHYSICAL_REFERENCE_VR1=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

 echo ============================================================
 echo M10 FINAL - VR1 POINT-KINETICS INDEPENDENT BENCHMARK
 echo ============================================================
 echo Explicit external model-assessment gate.
 echo Independent test-only C# Dormand-Prince reference.
 echo No production repair, exact-v9 parameter calibration, P3-R1,
 echo workload/protection change or second-long authorization.
 echo.

 echo [1/3] Static VR1 contract and VR0 prerequisite audit...
 powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-physical-reference-vr1.ps1"
 if errorlevel 1 goto :fail

 echo.
 echo [2/3] Ordinary Release gate identical to CI entry point...
 call eng\ci-ordinary.cmd
 if errorlevel 1 goto :fail

 echo.
 echo [3/3] Focused explicit VR1 point-kinetics model assessment...
 dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Reactor.Neutronics.M10FinalPhysicalReferenceVr1PointKineticsBenchmarkTests.GenericPointKineticsSolver_IsAssessedAgainstIndependentHebertSixGroupReference" --parallel none
 if errorlevel 1 goto :assessment_fail

 for %%F in (
   01-source-provenance-manifest.txt
   02-frozen-parameter-inputs.csv
   03-reference-selfcheck.csv
   04-reference-trajectory.csv
   05-production-trajectory.csv
   06-error-map.csv
   07-refinement-summary.csv
   08-deterministic-repeat.txt
   09-vr1-assessment-summary.txt
   10-impact-known-limitations.txt
 ) do if not exist "%REPORT_DIR%\%%F" goto :missing

 echo.
 echo M10 Final VR1 Point-Kinetics Independent Benchmark completed.
 echo Return the full "%REPORT_DIR%" folder before VR2, changing point-kinetics physics/tolerances, executing P3-R1, or authorizing a second replacement-long baseline.
 exit /b 0

:assessment_fail
 echo.
 echo M10 Final VR1 assessment did not meet its frozen gate contract.
 echo Preserve and return the complete "%REPORT_DIR%" folder before any repair or tolerance change.
 exit /b 1

:missing
 echo.
 echo M10 Final VR1 FAILED: expected artifacts are missing.
 echo Preserve and return any files already written under "%REPORT_DIR%".
 exit /b 1

:fail
 echo.
 echo M10 Final VR1 FAILED before focused assessment completion.
 echo Preserve and return any files already written under "%REPORT_DIR%".
 exit /b 1
