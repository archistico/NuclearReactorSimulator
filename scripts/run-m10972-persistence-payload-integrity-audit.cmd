@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "INFRA_PROJECT=tests\NuclearReactorSimulator.Infrastructure.Tests\NuclearReactorSimulator.Infrastructure.Tests.csproj"
set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.7.2 Hotfix 3 REV1 persistence payload integrity / JsonDocument exception-contract audit...
echo.
echo Schema v1 remains v1. Numeric command payload preservation is additive; string-enum migration and streaming remain deferred.
echo No replay authority, workstation activation, scoring, challenge, protection, command authority or physics change belongs to this gate.
echo.

if exist "artifacts\m10972-hotfix3-persistence-payload-integrity" rd /s /q "artifacts\m10972-hotfix3-persistence-payload-integrity"

findstr /C:"public double? NumericValue { get; set; }" "src\NuclearReactorSimulator.Infrastructure\Scenarios\Recording\JsonScenarioSessionArchiveSerializer.cs" >nul
if errorlevel 1 (
  echo ERROR: session-archive command document does not persist NumericValue.
  exit /b 1
)

findstr /C:"public ControlRoomCommand? OperatorCommand { get; set; }" "src\NuclearReactorSimulator.Infrastructure\Scenarios\Analysis\JsonPostIncidentAnalysisSerializer.cs" >nul
if not errorlevel 1 (
  echo ERROR: post-incident persistence still exposes the Application ControlRoomCommand type directly in its document DTO.
  exit /b 1
)

dotnet test --project "%INFRA_PROJECT%" --no-build -- --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests.Current_DescribesM10972Hotfix3Rev1JsonDocumentExceptionTypeAlignmentCandidate" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.2 Hotfix 3 REV1 artifact summary ===
if exist "artifacts\m10972-hotfix3-persistence-payload-integrity\01-m10972-hotfix3-persistence-payload-integrity-error-contract.summary.txt" (
  type "artifacts\m10972-hotfix3-persistence-payload-integrity\01-m10972-hotfix3-persistence-payload-integrity-error-contract.summary.txt"
) else (
  echo ERROR: expected M10.9.7.2 Hotfix 3 REV1 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.2 Hotfix 3 REV1 persistence payload integrity audit completed.
echo If build, ordinary tests and this focused gate are green, validate Hotfix 3 REV1 before M10.9.7.3 live Mission/Performance wiring.
exit /b 0
