@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\i5-water-steam-low-temperature-liquid-seam-audit"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running I.5 low-temperature liquid/saturation inverse-topology audit...
echo.
echo This maps the water-density-maximum region exposed by the Hotfix 8 5.01 C probe.
echo It distinguishes a real local saturated root from a production inverse-search miss.
echo No runtime physics, thermodynamic equation, coefficient, solver tolerance or operating gate is changed.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIWaterSteamLowTemperatureLiquidSeamAuditTests.LowTemperatureLiquidSeam_MapsDensityMaximumBlindSpotWithoutRuntimeChanges" ^
  --parallel none
set "EXITCODE=%ERRORLEVEL%"

if exist "%REPORT_DIR%\01-i5-water-steam-low-temperature-liquid-seam-audit.summary.txt" (
  echo.
  type "%REPORT_DIR%\01-i5-water-steam-low-temperature-liquid-seam-audit.summary.txt"
)

echo.
echo Detailed CSV files: "%REPORT_DIR%"
exit /b %EXITCODE%
