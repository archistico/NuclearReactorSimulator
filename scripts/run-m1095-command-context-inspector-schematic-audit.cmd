@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1
set "PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"
echo Running M10.9.5.3 COMMANDS context-inspector / schematic integration audit...
echo.
echo This gate validates presentation-only progressive disclosure, dependency-step selection and canonical mimic focus.
echo Command selection/navigation must not dispatch or mutate plant state; the existing explicit ENTER/EXECUTE boundary remains authoritative.
echo.
echo [1/2] ViewModel integration...
dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.ViewModels.OperatorComputerM10953CommandContextInspectorTests" ^
  --parallel none
if errorlevel 1 exit /b 1
echo.
echo [2/2] XAML contract...
dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.Views.OperatorComputerM10953ContextInspectorXamlTests" ^
  --parallel none
if errorlevel 1 exit /b 1
echo.
echo M10.9.5.3 COMMANDS context-inspector / schematic integration audit completed.
exit /b 0
