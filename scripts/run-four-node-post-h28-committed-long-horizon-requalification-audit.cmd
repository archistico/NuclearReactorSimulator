@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.24 Requalification 1 post-H.28 committed long-horizon / cross-profile gate...
echo.
echo H.28 validated performance evidence is fingerprint-checked first. The 30,000-interval domain is then rerun once against the optimized H.28 runtime.
echo Standard current-v2 remains ExplicitCommittedState at 10 ms. H.29 remains blocked until this gate is green.
echo.

echo Running descriptor and frozen-H.28 evidence contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodePostH28CommittedLongHorizonRequalificationAuditTests.FrozenH28Evidence_RetainsValidatedPerformanceCostOperationalSoakQualification" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h24-post-h28-four-node-committed-long-horizon-cross-profile-requalification"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Requalification heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodePostH28CommittedLongHorizonRequalificationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-post-h28-four-node-committed-long-horizon-cross-profile-requalification.summary.txt"
    "02-post-h28-committed-long-horizon-step-telemetry.csv"
    "03-post-h28-profile-qualification-metrics.csv"
    "04-post-h28-four-node-committed-long-horizon-requalification-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.24 post-H.28 requalification artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-post-h28-four-node-committed-long-horizon-cross-profile-requalification.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.24 Requalification 1 post-H.28 committed long-horizon / cross-profile gate completed.
exit /b 0
