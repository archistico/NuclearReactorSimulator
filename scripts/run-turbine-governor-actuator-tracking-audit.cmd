@echo off
setlocal
set "TEST_EXE=%~dp0..\tests\NuclearReactorSimulator.Application.Tests\bin\Debug\net10.0\NuclearReactorSimulator.Application.Tests.exe"
if not exist "%TEST_EXE%" (
    echo D.3 audit test executable not found. Run: dotnet build tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj
    exit /b 1
)
"%TEST_EXE%" -trait "Category=TurbineGovernorActuatorTrackingAudit" -explicit only -parallel none -showLiveOutput -reporter verbose
exit /b %errorlevel%