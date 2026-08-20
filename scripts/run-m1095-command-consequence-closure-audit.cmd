@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m1095-command-consequence-closure"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running M10.9.5.5 contextual command-consequence cumulative closure...
echo.
echo This gate adds no new runtime behavior. It reruns the four validated M10.9.5 focused gates,
echo then writes cumulative closure evidence. Final M10.9.5 promotion still requires the manual HMI checklist.
echo.

echo [1/5] M10.9.5.1 consequence catalog...
call scripts\run-m1095-command-consequence-catalog-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M1095_51_CATALOG_PASSED=1"

echo.
echo [2/5] M10.9.5.2 dependency-chain projection...
call scripts\run-m1095-command-dependency-chain-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M1095_52_DEPENDENCY_PASSED=1"

echo.
echo [3/5] M10.9.5.3 COMMANDS context-inspector / schematic integration...
call scripts\run-m1095-command-context-inspector-schematic-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M1095_53_CONTEXT_PASSED=1"

echo.
echo [4/5] M10.9.5.4 observed-response evidence...
call scripts\run-m1095-command-observed-response-audit.cmd
if errorlevel 1 exit /b 1
set "NRS_M1095_54_OBSERVED_PASSED=1"

echo.
echo [5/5] Writing cumulative M10.9.5 closure evidence...
set "NRS_M1095_CLOSURE_AUDIT=1"
dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.App.Tests.Views.OperatorComputerM10955CommandConsequenceClosureAuditTests.ValidatedFocusedEvidence_ClosesAutomatedM1095GatePendingManualHmiAcceptance" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-m1095-command-consequence-closure.summary.txt"
    "02-m1095-command-consequence-closure-gate-matrix.csv"
    "03-m1095-command-consequence-closure-contract.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected M10.9.5 closure artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-m1095-command-consequence-closure.summary.txt"
echo Detailed closure artifacts: "%REPORT_DIR%"
echo.
echo Automated M10.9.5 closure gate completed.
echo Final promotion requires: docs\M10_9_5_5_MANUAL_VALIDATION_CHECKLIST.md
exit /b 0
