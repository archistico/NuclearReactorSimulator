@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
set "OUT=artifacts\r3-h-scaled-search2"
if not exist "%OUT%" mkdir "%OUT%"
python eng\run-r3-h-scaled-search2.py static
if errorlevel 1 exit /b 1
dotnet build tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj --configuration Release --no-incremental --warnaserror > "%OUT%\00-build.log" 2>&1
if errorlevel 1 exit /b 1
dotnet test --project tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj --configuration Release --no-build --results-directory "%OUT%" --minimum-expected-tests 2 -- --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10R3HScaledSearch2Tests" --parallel none --report-xunit --report-xunit-filename preexecution.xml > "%OUT%\preexecution.log" 2>&1
if errorlevel 1 exit /b 1
python eng\run-r3-h-scaled-search2.py preexecution
if errorlevel 1 exit /b 1
python eng\run-r3-h-scaled-search2.py execute
if errorlevel 1 exit /b 1
python eng\run-r3-h-scaled-search2.py adjudicate
exit /b %errorlevel%
