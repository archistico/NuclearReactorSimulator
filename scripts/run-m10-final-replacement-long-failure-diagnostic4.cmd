@echo off
setlocal
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-failure-diagnostic4"
set "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC4=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"

echo M10 Final replacement-long failure Diagnostic 4
echo Diagnostic 3 PASS: SPEED seam ineffective while paralleled, valve preload supplied no material shaft margin, exact-v4 reproduced the failure family.
echo This gate changes no production source, default command policy, protection, exact-v9, mission pack or frozen replacement workload.
echo It discriminates load-step granularity from missing reactor/steam energy support before any repair.
echo.

echo [1/3] Restore...
dotnet restore NuclearReactorSimulator.sln
if errorlevel 1 goto :fail

echo [2/3] Build with warnings as errors...
dotnet build NuclearReactorSimulator.sln -c Debug --no-restore -warnaserror
if errorlevel 1 goto :fail

echo [3/3] Focused explicit load-ramp / torque-coupling / energy-support diagnostic...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongFailureDiagnostic4Tests.ExactV9_LoadRampTorqueCouplingAndEnergySupportCensus" --parallel none
if errorlevel 1 goto :fail

echo.
echo M10 Final replacement-long failure Diagnostic 4 completed.
echo Return the full "%REPORT_DIR%" folder before changing the replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
exit /b 0

:fail
echo.
echo M10 Final replacement-long failure Diagnostic 4 FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
