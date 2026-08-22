@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.8.1 REV1 Integrated Human / Automation / HMI validation matrix audit...
echo.
echo Scope: contract-only matrix freeze over M10.9.7.5 Hotfix 1 VALIDATED / M10.9.7 CLOSED.
echo REV1 changes no compiled/runtime source and no tests; it validates the matrix externally and reuses validated owner tests.
echo.

if exist "artifacts\m1098-integrated-validation-matrix" rd /s /q "artifacts\m1098-integrated-validation-matrix"

findstr /C:"M10.9.7.5 Hotfix 1" "src\NuclearReactorSimulator.Application\ApplicationDescriptor.cs" >nul
if errorlevel 1 (
  echo ERROR: compiled ApplicationDescriptor must remain on the validated M10.9.7.5 Hotfix 1 baseline in this contract-only slice.
  exit /b 1
)

findstr /S /M /C:"M10.9.8.1" "src\*.cs" "src\*.axaml" "tests\*.cs" >nul 2>nul
if not errorlevel 1 (
  echo ERROR: M10.9.8.1 REV1 must not add compiled/runtime or test-surface milestone markers under src/tests.
  exit /b 1
)

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.TrainingAssistanceAuthorityIndependenceTests" --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- --filter-class "NuclearReactorSimulator.Application.Tests.ControlRoom.Automation.PlantControlAuthorityIntegrationTests" --parallel none
if errorlevel 1 exit /b 1

where powershell >nul 2>nul
if errorlevel 1 (
  echo ERROR: Windows PowerShell is required for the M10.9.8.1 JSON contract validator.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10981-integrated-validation-matrix.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 exit /b 1

echo.
echo === M10.9.8.1 REV1 artifact summary ===
if exist "artifacts\m1098-integrated-validation-matrix\01-m10981-validation-matrix.summary.txt" (
  type "artifacts\m1098-integrated-validation-matrix\01-m10981-validation-matrix.summary.txt"
) else (
  echo ERROR: expected M10.9.8.1 REV1 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.8.1 REV1 automated validation-matrix audit completed.
echo Promotion also requires docs\M10_9_8_1_MATRIX_ACCEPTANCE_CHECKLIST.md.
exit /b 0
