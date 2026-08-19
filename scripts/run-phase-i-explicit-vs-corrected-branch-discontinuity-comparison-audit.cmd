@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-I.3 Hotfix 4 Classifier Fix 1 targeted-train branch-discontinuity comparison...
echo.
echo This diagnostic runs exact v2 and exact v3 for 100 simulated seconds each at 10 ms resolution and classifies reverse flow across stop/control/admission.
echo It does not change H.30 policy and does not freeze I.3 tolerance budgets.
echo.

set "REPORT_DIR=%CD%\artifacts\i3-hotfix4-explicit-vs-corrected-branch-discontinuity-comparison"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

rem This is a scheduled/manual long diagnostic. The environment opt-in prevents accidental
rem execution inside ordinary dotnet test, while --explicit only selects the explicit audit.
set "NRS_I3_BRANCH_COMPARISON_AUDIT=1"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIExplicitVsCorrectedBranchDiscontinuityComparisonAuditTests.ExactV2ExplicitVsV3Corrected_ClassifiesTurbineInletFlowDirectionDiscontinuitiesAtTenMillisecondResolution" ^
    --parallel none
set "EXITCODE=%ERRORLEVEL%"
set "NRS_I3_BRANCH_COMPARISON_AUDIT="

if exist "%REPORT_DIR%\01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt" (
  echo.
  type "%REPORT_DIR%\01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt"
)

if not "%EXITCODE%"=="0" exit /b %EXITCODE%

for %%F in (
    "01-phase-i-v2-v3-branch-discontinuity-comparison.summary.txt"
    "02-v2-v3-ten-millisecond-trace.csv"
    "03-generation-drop-comparison.csv"
    "04-drop-episodes.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected I.3 Hotfix 4 comparison artifact missing: %%F
        exit /b 2
    )
)

echo.
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-I.3 Hotfix 4 Classifier Fix 1 targeted-train branch-discontinuity comparison completed.
exit /b 0
