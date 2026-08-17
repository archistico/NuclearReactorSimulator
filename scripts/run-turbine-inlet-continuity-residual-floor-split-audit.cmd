@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.18 turbine-inlet continuity extension and residual-floor split diagnosis...

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.TurbineInletContinuityResidualFloorSplitAuditTests.FrozenH17Evidence_RetainsValidatedSplitContract" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h18-turbine-inlet-continuity-residual-floor-split"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.18 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=TurbineInletContinuityResidualFloorSplitAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-current-v2-turbine-inlet-continuity-residual-floor-split.summary.txt"
    "02-frozen-h17-evidence-selection.csv"
    "03-four-node-policy-results.csv"
    "04-four-node-recovery-matrix.csv"
    "05-remaining-failure-residual-floor-ranking.csv"
    "06-remaining-failure-inverse-branch-scan.csv"
    "07-turbine-inlet-committed-transparency.csv"
    "08-h18-split-diagnosis-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.18 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-current-v2-turbine-inlet-continuity-residual-floor-split.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.18 turbine-inlet continuity / residual-floor split diagnosis audit passed.
exit /b 0
