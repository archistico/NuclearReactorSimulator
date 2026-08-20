@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\i5-synchronization-corrected-v3-qualification"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running I.5 exact synchronization v3 corrected-commit qualification...
echo.
echo Exact pre-synchronization-grid-loading@1/@2 remain frozen and are not reinterpreted.
echo Exact @3 preserves the @2 physical/control/grid seed and changes only hydraulic numerical ownership.
echo The 10 s checkpoint is bounded stabilization; the sustained 20-60 s floor remains greater than 4.0 MWe with rotor 2990-3010 rpm.
echo This is a qualification gate only; desktop production policy and the cumulative I.5 closure remain unchanged.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseISynchronizationCorrectedExactVersionQualificationAuditTests.ExactVersion3_CorrectedCommit_QualifiesBoundedStabilizationAndStrictSustainedLowLoadJourney" ^
  --parallel none
set "EXITCODE=%ERRORLEVEL%"

if exist "%REPORT_DIR%\01-i5-synchronization-corrected-v3-qualification.summary.txt" (
  echo.
  type "%REPORT_DIR%\01-i5-synchronization-corrected-v3-qualification.summary.txt"
)

echo.
echo Detailed qualification artifacts: "%REPORT_DIR%"
exit /b %EXITCODE%
