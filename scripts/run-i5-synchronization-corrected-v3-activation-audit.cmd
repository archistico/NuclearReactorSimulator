@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\i5-synchronization-corrected-v3-activation"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"

echo Running I.5 synchronization corrected-v3 activation contract...
echo.
echo Hotfix 5 qualification is frozen validated evidence.
echo Exact @1/@2 remain loadable and unchanged; @3 is registered as supported current for sustained synchronization.
echo This fast gate does not rerun the 60 s journey; gameplay-long owns that scheduled runtime regression.
echo.

dotnet test --project "%PROJECT%" -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseISynchronizationCorrectedExactVersionActivationAuditTests.ValidatedQualification_ActivatesExactV3RegistryAndLongJourneyContract" ^
  --parallel none
if errorlevel 1 exit /b 1

for %%F in (
    "00-progress.txt"
    "01-i5-synchronization-corrected-v3-activation.summary.txt"
    "02-synchronization-exact-version-activation-matrix.csv"
) do (
    if not exist "%REPORT_DIR%\%%~F" (
        echo ERROR: expected I.5 synchronization activation artifact missing: %%~F
        exit /b 2
    )
)

echo.
type "%REPORT_DIR%\01-i5-synchronization-corrected-v3-activation.summary.txt"
echo Detailed activation artifacts: "%REPORT_DIR%"
echo I.5 synchronization corrected-v3 activation gate completed.
exit /b 0
