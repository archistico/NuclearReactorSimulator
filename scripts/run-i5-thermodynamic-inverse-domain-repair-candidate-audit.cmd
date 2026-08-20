@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\i5-thermodynamic-inverse-domain-repair-candidate"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running I.5 thermodynamic inverse-domain repair-candidate audit...
echo.
echo This is an opt-in repair candidate only. Registered/default runtimes remain on the historical closure.
echo It verifies repaired vapor/liquid inverse topology and the frozen desktop load raise/lower journey under explicit and corrected hydraulics.
echo No production initial-condition version, hydraulic policy, acceptance floor or fail-closed rule is activated or weakened.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIThermodynamicInverseDomainRepairCandidateAuditTests" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "topology\01-i5-thermodynamic-inverse-domain-repair-topology.summary.txt"
    "topology\02-repaired-vapor-seam.csv"
    "topology\03-repaired-observed-gap-probes.csv"
    "operational\04-repaired-operational-journey-matrix.csv"
    "operational\05-repaired-operational-journey-checkpoints.csv"
    "operational\06-i5-thermodynamic-inverse-domain-repair-operational.summary.txt"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected repair-candidate artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\topology\01-i5-thermodynamic-inverse-domain-repair-topology.summary.txt"
echo.
type "%REPORT_DIR%\operational\06-i5-thermodynamic-inverse-domain-repair-operational.summary.txt"
echo.
echo Detailed CSV files: "%REPORT_DIR%"
exit /b 0
