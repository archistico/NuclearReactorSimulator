@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.25 committed protection / operational-transient matrix gate...
echo.
echo H.25 changes no committed numerical runtime and does NOT rerun the 4.5-hour H.24 qualification.
echo Compact user-validated H.24 evidence is frozen and fingerprint-checked first.
echo.

echo Running H.25 descriptor, frozen-H.24 evidence and protection-catalogue contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeCommittedProtectionOperationalTransientMatrixAuditTests.FrozenH24Evidence_RetainsValidatedCommittedLongHorizonQualification" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeCommittedProtectionOperationalTransientMatrixAuditTests.CorrectedCommitCurrentV2_RetainsExpectedProtectionFunctionCatalogue" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h25-four-node-committed-protection-operational-transient-matrix"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.25 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.
echo NOTE: H.25 is intentionally targeted and short. H.24 is a rare qualification gate and is not chained here.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeCommittedProtectionOperationalTransientMatrixAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-committed-protection-transient-matrix.summary.txt"
    "02-protection-transient-matrix-step-telemetry.csv"
    "03-protection-transient-matrix-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.25 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-committed-protection-transient-matrix.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.25 committed protection / operational-transient matrix gate completed.
exit /b 0
