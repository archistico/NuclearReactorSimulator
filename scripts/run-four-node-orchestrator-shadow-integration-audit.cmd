@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.21 four-node orchestrator shadow wiring / telemetry integration gate...
echo.
echo H.21 prerequisite 1/2: rerun the complete validated H.19 long-horizon/cross-profile qualification.
call "scripts\run-four-node-long-horizon-cross-profile-qualification-audit.cmd"
if errorlevel 1 exit /b 1

echo.
echo H.21 prerequisite 2/2: rerun the validated H.20 fail-closed authority/rollback contract.
call "scripts\run-four-node-activation-rollback-contract-audit.cmd"
if errorlevel 1 exit /b 1

echo.
echo Running H.21 ordinary focused contracts...
dotnet test --project "tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Domain.Tests.Plant.HydraulicNumericalCouplingDefinitionTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeOrchestratorShadowIntegrationAuditTests.FrozenH20Evidence_RetainsValidatedFailClosedActivationContract" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h21-four-node-orchestrator-shadow-integration"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.21 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeOrchestratorShadowIntegrationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-orchestrator-shadow-integration.summary.txt"
    "02-step-telemetry.csv"
    "03-four-node-orchestrator-shadow-integration-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.21 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-orchestrator-shadow-integration.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.21 four-node orchestrator shadow wiring / telemetry integration gate completed.
exit /b 0
