@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-I.3 Hotfix 5 corrected 300-second healthy reference requalification...
echo.
echo This scheduled/manual gate runs exact v3 corrected-commit for 300 simulated seconds.
echo Every 10 ms step is checked for generation health and stop/control/admission reverse flow.
echo H.30 remains OPT-IN ONLY; this gate can only unblock a separate policy re-review.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseICorrectedHealthyReferenceRequalificationAuditTests.FrozenValidatedHotfix4Classifier_ProvesCorrectedSuppressionBeforeThreeHundredSecondRun" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseICorrectedHealthyReferenceRequalificationAuditTests.CorrectedThreeHundredSecondContract_IsExactV3AndPolicyNeutral" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\i3-hotfix5-corrected-300s-healthy-reference-requalification"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
set "NRS_I3_CORRECTED_300S_AUDIT=1"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseICorrectedHealthyReferenceRequalificationAuditTests.ExactV3Corrected_ThreeHundredSeconds_RemainsHealthyContinuousConservativeAndDeterministic" ^
    --parallel none
set "EXITCODE=%ERRORLEVEL%"
set "NRS_I3_CORRECTED_300S_AUDIT="

if exist "%REPORT_DIR%\01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt" (
    echo.
    type "%REPORT_DIR%\01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt"
)
if not "%EXITCODE%"=="0" exit /b %EXITCODE%

for %%F in (
    "00-progress.txt"
    "01-phase-i-corrected-300s-healthy-reference-requalification.summary.txt"
    "02-corrected-300s-reference-contract.csv"
    "03-corrected-reference-trajectory-samples.csv"
    "04-corrected-final-window-slopes.csv"
    "05-corrected-step-health-violations.csv"
    "06-corrected-targeted-reverse-flow-violations.csv"
    "07-corrected-production-telemetry.csv"
    "08-determinism-control.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected I.3 Hotfix 5 artifact missing: %%F
        exit /b 2
    )
)

echo.
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-I.3 Hotfix 5 corrected 300-second healthy reference requalification completed.
exit /b 0
