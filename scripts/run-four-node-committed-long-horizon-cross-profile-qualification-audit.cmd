@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.24 committed long-horizon / cross-profile qualification gate...
echo.
echo H.24 makes no numerical runtime changes. The user-validated H.23 focused artifacts are frozen and fingerprint-checked instead of rerunning H.23/H.22/H.21/H.19 prerequisites.
echo.

echo Running H.24 ordinary descriptor and frozen-H.23 evidence contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeCommittedLongHorizonCrossProfileQualificationAuditTests.FrozenH23Evidence_RetainsValidatedReplayCheckpointProtectionQualification" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h24-four-node-committed-long-horizon-cross-profile-qualification"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.24 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.
echo NOTE: this is the first full 30,000-interval committed-path qualification and is intentionally much heavier than H.23.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeCommittedLongHorizonCrossProfileQualificationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-committed-long-horizon-cross-profile.summary.txt"
    "02-committed-long-horizon-step-telemetry.csv"
    "03-profile-qualification-metrics.csv"
    "04-four-node-committed-long-horizon-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.24 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-committed-long-horizon-cross-profile.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.24 committed long-horizon / cross-profile qualification gate completed.
exit /b 0
