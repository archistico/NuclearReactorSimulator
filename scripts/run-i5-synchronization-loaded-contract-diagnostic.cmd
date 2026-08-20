@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\i5-synchronization-loaded-contract-diagnostic"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running I.5 synchronization loaded-contract blocker diagnostic...
echo.
echo Exact pre-synchronization-grid-loading@2 is frozen and is not modified.
echo The diagnostic separates the loaded main-steam capacity / stop-out grade, corrected-commit hydraulics and desktop PID.
echo It preserves the strict long-journey acceptance floor and registers no new initial-condition version.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseISynchronizationLoadedContractDiagnosticTests.FrozenV2AndBoundedLoadedContracts_ClassifySixtySecondLowLoadStability" ^
  --parallel none
set "EXITCODE=%ERRORLEVEL%"

if exist "%REPORT_DIR%\01-i5-synchronization-loaded-contract-diagnostic.summary.txt" (
  echo.
  type "%REPORT_DIR%\01-i5-synchronization-loaded-contract-diagnostic.summary.txt"
)

echo.
echo Detailed CSV files: "%REPORT_DIR%"
exit /b %EXITCODE%
