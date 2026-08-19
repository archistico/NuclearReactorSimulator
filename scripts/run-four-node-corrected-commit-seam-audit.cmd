@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.22 four-node corrected-candidate commit-seam gate...
echo.
echo H.22 prerequisite: rerun the complete validated H.21 orchestrator sidecar gate.
echo H.21 itself reruns the validated H.19 and H.20 prerequisite gates.
call "scripts\run-four-node-orchestrator-shadow-integration-audit.cmd"
if errorlevel 1 exit /b 1

echo.
echo Running H.22 ordinary commit-seam contracts...
dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Domain.Tests.Plant.HydraulicNumericalCouplingDefinitionTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.FourNodeBranchContinuityCorrectedCommitSeamTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeCorrectedCommitIntegrationAuditTests.FrozenH21Evidence_RetainsValidatedTrajectoryTransparentOrchestratorWiring" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h22-four-node-corrected-commit-seam"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.22 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeCorrectedCommitIntegrationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-corrected-commit-seam.summary.txt"
    "02-step-commit-telemetry.csv"
    "03-four-node-corrected-commit-seam-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.22 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-corrected-commit-seam.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.22 four-node corrected-candidate commit-seam gate completed.
exit /b 0
