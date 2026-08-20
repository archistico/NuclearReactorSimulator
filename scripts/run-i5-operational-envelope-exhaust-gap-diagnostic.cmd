@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\i5-operational-envelope-exhaust-gap-diagnostic"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running I.5 operational-envelope exhaust correlation-gap diagnostic...
echo.
echo This reproduces the exact desktop load raise/lower journey once under exact @2 explicit and once under @3 corrected-commit.
echo No production runtime, thermodynamic equation, condenser coefficient, four-node target set or acceptance threshold is changed.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIOperationalEnvelopeExhaustCorrelationGapDiagnosticTests.ExactV2AndV3_LoadRaiseLower_ClassifyExhaustEnvelopeFailureWithoutRuntimeChanges" ^
  --parallel none
set "EXITCODE=%ERRORLEVEL%"

if exist "%REPORT_DIR%\01-i5-operational-envelope-exhaust-gap-diagnostic.summary.txt" (
  echo.
  type "%REPORT_DIR%\01-i5-operational-envelope-exhaust-gap-diagnostic.summary.txt"
)

echo.
echo Detailed CSV files: "%REPORT_DIR%"
exit /b %EXITCODE%
