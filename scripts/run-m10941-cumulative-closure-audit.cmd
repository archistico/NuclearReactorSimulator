@echo off
setlocal EnableExtensions

set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\i5-m10941-cumulative-closure"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running final M10.9.4.1-I.5 repaired exact-v4 cumulative closure...
echo.
echo This is the Phase-I closure gate and can take multiple hours on local hardware.
echo It runs ordinary/current evidence, then the complete scheduled-long matrix, including the repaired exact-v4 300 s reference regression against the unchanged frozen I.3 budgets.
echo Historical exact @3 H.30/I.3/I.4 evidence remains immutable provenance; exact @2 remains fail-closed rollback/reference.
echo No runtime physics, numerical tolerance, P060-F040 contract, bounded hysteresis limit or 10 ms fixed step is retuned by this closure script.
echo.

echo [I.5 1/3] Running ordinary CI plus repaired-v4 current evidence...
call eng\ci-ordinary.cmd
if errorlevel 1 exit /b 1
set "NRS_I5_ORDINARY_CI_PASSED=1"

echo.
echo [I.5 2/3] Running final scheduled-long matrix...
call eng\ci-long.cmd
if errorlevel 1 exit /b 1
set "NRS_I5_LONG_GATES_PASSED=1"

echo.
echo [I.5 3/3] Writing final evidence-derived repaired-v4 cumulative closure...
set "NRS_I5_CLOSURE_AUDIT=1"
dotnet test --project "%PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseICumulativeM10941ClosureAuditTests.ValidatedCurrentAndLongEvidence_ClosesM10941AndPhaseIOnRepairedExactV4" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-m10941-cumulative-closure.summary.txt"
    "02-m10941-cumulative-closure-gate-matrix.csv"
    "03-m10941-cumulative-closure-metrics.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected I.5 final closure artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-m10941-cumulative-closure.summary.txt"
echo Detailed closure artifacts: "%REPORT_DIR%"
echo M10.9.4.1 / Phase-I repaired exact-v4 cumulative closure gate completed.
exit /b 0
