@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-materiality-diagnostic1-adjudication"
set "NRS_M10_FINAL_PHYSICAL_REFERENCE_VR2_MATERIALITY_ADJUDICATION=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo ============================================================
echo M10 FINAL - VR2 MATERIALITY DIAGNOSTIC 1 RETURNED EVIDENCE ADJUDICATION
echo ============================================================
echo Adjudicates the complete returned Attempt-5 evidence without replaying the 3600 s path.
echo The original 1e-9 failure is preserved as historical evidence.
echo No production repair, thermodynamic tolerance change, exact-v9 change, VR3,
echo P3-R1 execution or second replacement-long authorization.
echo.

echo [1/3] Static returned Attempt-5 evidence and adjudication contract audit...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-replanning-materiality-diagnostic1-adjudication.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/3] Ordinary Release gate identical to CI entry point...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [3/3] Focused returned-evidence adjudication against exact-v9 H.22 fixed-point residual contract...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.M10FinalPhysicalReferenceVr2MaterialityReturnedEvidenceAdjudicationTests.ReturnedAttempt5_Evidence_IsAdjudicatedAgainstAuthoritativeExactV9FixedPointResidualContract" --parallel none
if errorlevel 1 goto :adjudication_fail

if not exist "%REPORT_DIR%\01-returned-evidence-adjudication.txt" goto :missing

echo.
echo M10 Final VR2 Materiality Diagnostic 1 returned-evidence adjudication completed.
echo This closes only the diagnostic-evidence interpretation; it does not authorize repair or VR3.
echo Return the full "%REPORT_DIR%" folder for the separate engineering repair-planning decision.
exit /b 0

:adjudication_fail
echo.
echo M10 Final VR2 Materiality Diagnostic 1 returned-evidence adjudication FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%" before any repair, tolerance change or VR3.
exit /b 1

:missing
echo.
echo M10 Final VR2 Materiality Diagnostic 1 returned-evidence adjudication FAILED: expected adjudication artifact is missing.
exit /b 1

:fail
echo.
echo M10 Final VR2 Materiality Diagnostic 1 returned-evidence adjudication FAILED before focused adjudication completion.
exit /b 1
