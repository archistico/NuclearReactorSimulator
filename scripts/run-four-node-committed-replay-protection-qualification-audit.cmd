@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.23 deterministic replay / checkpoint / protection qualification gate...
echo.
echo H.23 makes no numerical runtime changes. The user-validated H.22 focused artifacts are frozen and fingerprint-checked instead of rerunning the expensive H.22/H.21/H.19 chain.
echo.

echo Running H.23 ordinary descriptor and frozen-H.22 evidence contracts...
dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.FourNodeCommittedReplayProtectionQualificationAuditTests.FrozenH22Evidence_RetainsValidatedCorrectedCommitSeam" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h23-four-node-committed-replay-protection-qualification"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo H.23 heartbeat will be written to:
echo   "%REPORT_DIR%\00-progress.txt"
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=FourNodeCommittedReplayProtectionQualificationAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-four-node-committed-replay-checkpoint-protection.summary.txt"
    "02-replay-protection-trace.csv"
    "03-four-node-committed-replay-protection-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.23 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-four-node-committed-replay-checkpoint-protection.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.23 deterministic replay / checkpoint / protection qualification gate completed.
exit /b 0
