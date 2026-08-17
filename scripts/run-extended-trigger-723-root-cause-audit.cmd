@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo Running M10.9.4.1-H.15 extended trigger 723 root-cause diagnosis...

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" -- ^
    --filter-class "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests" ^
    --parallel none
if errorlevel 1 exit /b 1

set "REPORT_DIR=%CD%\artifacts\h15-extended-trigger-723-root-cause"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
    --explicit only ^
    --filter-trait "Category=ExtendedTrigger723RootCauseAudit" ^
    --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "01-current-v2-extended-trigger-723-root-cause.summary.txt"
    "02-neighborhood-solver-results.csv"
    "03-node-fixed-point-residual-ranking.csv"
    "04-all-hydraulic-path-probes.csv"
    "05-all-thermodynamic-node-probes.csv"
    "06-all-node-inverse-branch-selection.csv"
    "07-all-node-inverse-branch-candidates.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected H.15 audit artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-current-v2-extended-trigger-723-root-cause.summary.txt"
echo Detailed CSV files: "%REPORT_DIR%"
echo M10.9.4.1-H.15 extended trigger 723 root-cause diagnosis audit passed.
exit /b 0
