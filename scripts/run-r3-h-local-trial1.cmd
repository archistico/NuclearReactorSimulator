@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
set "OUT=artifacts\r3-h-local-trial1"
if not exist "%OUT%" mkdir "%OUT%"
python eng\validate-r3-h-local-trial1.py static
if errorlevel 1 exit /b 1
dotnet build tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj --configuration Release --no-incremental --warnaserror > "%OUT%\00-build.log" 2>&1
if errorlevel 1 exit /b 1
dotnet test --project tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj --configuration Release --no-build --results-directory "%OUT%" --minimum-expected-tests 3 -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3HLocalModelTrial1Tests" --parallel none --report-xunit --report-xunit-filename preexecution.xml > "%OUT%\preexecution.log" 2>&1
if errorlevel 1 exit /b 1
python eng\validate-r3-h-local-trial1.py preexecution
if errorlevel 1 exit /b 1
set "NRS_R3_H_LOCAL_TRIAL1=1"
dotnet test --project tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj --configuration Release --no-build --results-directory "%OUT%" --minimum-expected-tests 1 -- --explicit only --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3HLocalModelTrial1Tests.ExecuteOnlyReviewedFourteenRecipes" --parallel none --report-xunit --report-xunit-filename execution.xml > "%OUT%\execution.log" 2>&1
if errorlevel 1 exit /b 1
set "NRS_R3_H_LOCAL_TRIAL1="
python eng\validate-r3-h-local-trial1.py adjudicate
exit /b %errorlevel%
