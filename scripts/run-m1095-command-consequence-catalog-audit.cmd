@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.5.1 contextual command-consequence catalog audit...
echo.
echo This gate validates authored qualitative command semantics only.
echo It does not dispatch commands, change plant state, predict numeric outcomes or add new permissive/protection ownership.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer.OperatorComputerM10951CommandConsequenceCatalogTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo M10.9.5.1 contextual command-consequence catalog audit completed.
exit /b 0
