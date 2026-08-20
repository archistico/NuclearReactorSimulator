@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1
set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
echo Running M10.9.5.2 explicit dependency-chain projection audit...
echo.
echo This gate validates authored bounded command dependency chains only.
echo It performs no automatic graph traversal, dispatches no command and changes no plant state.
echo.
dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer.OperatorComputerM10952CommandDependencyChainProjectionTests" ^
  --parallel none
if errorlevel 1 exit /b 1
echo.
echo M10.9.5.2 explicit dependency-chain projection audit completed.
exit /b 0
