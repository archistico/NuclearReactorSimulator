@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.28 performance / cost / operational-soak gate...
echo.
echo H.28 does NOT rerun the 4.5-hour H.24 qualification.
echo User-validated H.27 and H.28.1-G evidence are frozen and fingerprint-checked first.
echo Wall-clock evidence is paired corrected-vs-explicit on the same machine; 10 ms remains simulated fixed-step time, not an xUnit wall-clock deadline.
echo.

echo Running H.28 descriptor and frozen-H.27 evidence contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodePerformanceOperationalSoakAuditTests.FrozenH27Evidence_RetainsValidatedBoundedOffDesignEnvelope" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodePerformanceOperationalSoakAuditTests.FrozenH281GEvidence_RetainsValidatedCpuTailClosure" ^
    --parallel none
if errorlevel 1 exit /b 1

echo Running unchanged H.22 corrected-commit authority seam contract...
dotnet test --project "tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Simulation.Tests.Plant.FourNodeBranchContinuityCorrectedCommitSeamTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h28-four-node-performance-cost-operational-soak"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.28 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.
echo NOTE: this is a bounded performance/soak gate. H.24 remains frozen evidence unless numerical runtime changes or closure explicitly requires it.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodePerformanceOperationalSoakAudit" ^
    --parallel none
if errorlevel 1 (
    echo.
    if exist "%REPORT_DIR%\01-four-node-performance-cost-operational-soak.summary.txt" (
        echo H.28 produced diagnostic evidence before failing its qualification gate:
        type "%REPORT_DIR%\01-four-node-performance-cost-operational-soak.summary.txt"
        echo Detailed CSV files: "%REPORT_DIR%"
    )
    exit /b 1
)

for %%F in (
    "00-progress.txt"
    "01-four-node-performance-cost-operational-soak.summary.txt"
    "02-performance-benchmark.csv"
    "03-operational-soak-samples.csv"
    "04-performance-cost-soak-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.28 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-performance-cost-operational-soak.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.28 performance / cost / operational-soak gate completed.
exit /b 0
