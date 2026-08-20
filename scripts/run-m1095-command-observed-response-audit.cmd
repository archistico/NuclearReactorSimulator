@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1
set "APP_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "UI_PROJECT=tests\NuclearReactorSimulator.App.Tests\NuclearReactorSimulator.App.Tests.csproj"

echo Running M10.9.5.4 observed-response evidence audit...
echo.
echo This gate validates deterministic post-dispatch evidence over the authored M10.9.5.1 monitor set.
echo Observation uses logical simulation steps only, rejected commands show no fictional plant effects,
echo and numeric deltas never become generic SUCCESS/FAILURE or causal claims.
echo.
echo [1/3] Application observation projection / bounded accumulator...
dotnet test --project "%APP_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.OperatorComputer.OperatorComputerM10954ObservedResponseEvidenceTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo [2/3] ViewModel accepted/rejected dispatch evidence...
dotnet test --project "%UI_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.ViewModels.OperatorComputerM10954ObservedResponseViewModelTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo [3/3] XAML separation contract...
dotnet test --project "%UI_PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.App.Tests.Views.OperatorComputerM10954ObservedResponseXamlTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo M10.9.5.4 observed-response evidence audit completed.
exit /b 0
