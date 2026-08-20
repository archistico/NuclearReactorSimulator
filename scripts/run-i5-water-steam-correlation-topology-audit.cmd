@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\i5-water-steam-correlation-topology-audit"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running I.5 water/steam correlation topology audit...
echo.
echo This maps the full saturated-vapor / superheated seam and probes the liquid / saturation seam.
echo It also classifies the @2, @3 and historical M9.7 no-root points against the same production model.
echo No runtime physics, thermodynamic equation, coefficient, solver tolerance, target set or acceptance gate is changed.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIWaterSteamCorrelationTopologyAuditTests.WaterSteamPhaseBoundaryTopology_MapsGapOverlapAndLiquidContinuityWithoutRuntimeChanges" ^
  --parallel none
set "EXITCODE=%ERRORLEVEL%"

if exist "%REPORT_DIR%\01-i5-water-steam-correlation-topology-audit.summary.txt" (
  echo.
  type "%REPORT_DIR%\01-i5-water-steam-correlation-topology-audit.summary.txt"
)

echo.
echo Detailed CSV files: "%REPORT_DIR%"
exit /b %EXITCODE%
