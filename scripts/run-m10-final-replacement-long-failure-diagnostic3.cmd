@echo off
setlocal
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-failure-diagnostic3"
set "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC3=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"

echo M10 Final replacement-long failure Diagnostic 3
echo Diagnostic 2 PASS: Assisted rod coordination did not improve the exact-v9 5-to-10 MWe loss-of-synchronism path.
echo This gate changes no production source, protection threshold, exact-v9, mission pack or frozen replacement workload.
echo It closes the remaining paralleled turbine-governing seam, probes physical valve preloading, and compares exact-v4 vs exact-v9.
echo.

echo [1/3] Restore...
dotnet restore NuclearReactorSimulator.sln
if errorlevel 1 goto :fail

echo [2/3] Build with warnings as errors...
dotnet build NuclearReactorSimulator.sln -c Debug --no-restore -warnaserror
if errorlevel 1 goto :fail

echo [3/3] Focused explicit turbine-governing / mechanical-preload / version discrimination audit...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongFailureDiagnostic3Tests.ExactV9_TurbineGoverningValvePreloadAndHistoricalVersionDiscriminationCensus" --parallel none
if errorlevel 1 goto :fail

echo.
echo M10 Final replacement-long failure Diagnostic 3 completed.
echo Return the full "%REPORT_DIR%" folder before changing the replacement workload, authority policy, generator-load semantics, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
exit /b 0

:fail
echo.
echo M10 Final replacement-long failure Diagnostic 3 FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
