@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
set "OUT=artifacts\r3-stage-energy-law-reconstruction2"
if not exist "%OUT%" mkdir "%OUT%"
python eng\run-r3-stage-energy-law-reconstruction2.py static
if errorlevel 1 exit /b 1
dotnet build tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj --configuration Release --no-incremental --warnaserror > "%OUT%\00-build.log" 2>&1
if errorlevel 1 exit /b 1
python eng\run-r3-stage-energy-law-reconstruction2.py preexecution
if errorlevel 1 exit /b 1
python eng\run-r3-stage-energy-law-reconstruction2.py execute
if errorlevel 1 exit /b 1
python eng\run-r3-stage-energy-law-reconstruction2.py adjudicate
exit /b %errorlevel%
