@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
set "OUT=%CD%\artifacts\r3-h-feasibility1"
if not exist "%OUT%" mkdir "%OUT%"
rem Invalidate only named outputs owned by this execution before any build/test.
for %%F in (00-static.log 00-build.log 01-preexecution-tests.log 01-provenance.json 02-baseline.json 03-preexecution-review.json preexecution.xml 04-search-trace.jsonl 05-search-result.json 06-confirmation.json 07-execution-summary.json 08-manifest.csv 09-search-test.log execution.xml) do if exist "%OUT%\%%F" del /q "%OUT%\%%F"
echo [1/5] Freeze / prior-test / production audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-r3-h-feasibility1.ps1" > "%OUT%\00-static.log" 2>&1
if errorlevel 1 goto :fail
echo [2/5] Fresh focused Release build, warnings as errors...
dotnet --info > "%OUT%\00-build.log" 2>&1
if errorlevel 1 goto :fail
dotnet build ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-incremental --warnaserror >> "%OUT%\00-build.log" 2>&1
if errorlevel 1 goto :fail
echo [3/5] Baseline identity, bounded-solver tests and preexecution review...
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --results-directory "%OUT%" --minimum-expected-tests 6 -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3HConservedInventoryFeasibility1Tests" --parallel none --report-xunit --report-xunit-filename preexecution.xml > "%OUT%\01-preexecution-tests.log" 2>&1
if errorlevel 1 goto :fail
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-r3-h-feasibility1.ps1" -Stage Preexecution
if errorlevel 1 goto :fail
echo [4/5] Execute the unchanged bounded H search...
set "NRS_R3_H_FEASIBILITY1=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --results-directory "%OUT%" --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3HConservedInventoryFeasibility1Tests.ExecuteFrozenHFeasibilitySearch_AfterSuccessfulPreexecutionReview" --parallel none --report-xunit --report-xunit-filename execution.xml > "%OUT%\09-search-test.log" 2>&1
if errorlevel 1 goto :fail
set "NRS_R3_H_FEASIBILITY1="
echo [5/5] Independently adjudicate workload / trace / classification...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-r3-h-feasibility1.ps1" -Stage Adjudicate
if errorlevel 1 goto :fail
echo H feasibility evidence complete. Review artifacts\r3-h-feasibility1. R3 remains RED.
exit /b 0
:fail
set "NRS_R3_H_FEASIBILITY1="
echo H feasibility FAILED technically. Inspect owned logs; do not retune or start R3 qualification.
exit /b 1
