@echo off
setlocal
cd /d "%~dp0\.."

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10-final-replacement-long-failure-diagnostic2"
set "NRS_M10_FINAL_REPLACEMENT_LONG_DIAGNOSTIC2=1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%"

echo M10 Final replacement-long failure Diagnostic 2
echo Diagnostic 1 PASS: generator loss-of-synchronism owns the shared RL-M1/RL-R1 load-raise trip.
echo This gate changes no production source, protection threshold, exact-v9, mission pack or frozen replacement workload.
echo It discriminates SupervisoryAutomatic command suppression from Assisted M7.6-style coordinated load manoeuvres.
echo.

echo [1/3] Restore...
dotnet restore NuclearReactorSimulator.sln
if errorlevel 1 goto :fail

echo [2/3] Build with warnings as errors...
dotnet build NuclearReactorSimulator.sln -c Debug --no-restore -warnaserror
if errorlevel 1 goto :fail

echo [3/3] Focused explicit authority/coordination discrimination audit...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalReplacementLongFailureDiagnostic2Tests.ExactV9_LoadRaiseAuthorityAndM76CoordinationDiscriminationCensus" --parallel none
if errorlevel 1 goto :fail

echo.
echo M10 Final replacement-long failure Diagnostic 2 completed.
echo Return the full "%REPORT_DIR%" folder before changing the replacement workload, authority policy, protection semantics, exact-v9 runtime, mission pack, or freezing a second replacement-long baseline.
exit /b 0

:fail
echo.
echo M10 Final replacement-long failure Diagnostic 2 FAILED.
echo Preserve and return any files already written under "%REPORT_DIR%".
exit /b 1
