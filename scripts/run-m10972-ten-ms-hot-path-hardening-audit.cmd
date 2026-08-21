@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "DOMAIN_PROJECT=tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj"
set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.7.2 Hotfix 2 REV1 measured 10 ms hot-path allocation/lookup hardening audit...
echo.
echo Primary evidence is allocation elimination plus same-process relative hot-path improvement.
echo No solver retuning, reference-plant coefficient change, MISSION activation, scoring, challenge or command authority belongs to this gate.
echo.

if exist "artifacts\m10972-hotfix2-ten-ms-hot-path" rd /s /q "artifacts\m10972-hotfix2-ten-ms-hot-path"

findstr /C:"ObservationFingerprint()" "src\NuclearReactorSimulator.Application\Scenarios\Challenges\ScenarioChallengeTracker.cs" >nul
if not errorlevel 1 (
  echo ERROR: ScenarioChallengeTracker still contains ObservationFingerprint string materialization.
  exit /b 1
)

findstr /C:"Dictionary<" "src\NuclearReactorSimulator.Domain\Plant\PlantState.cs" >nul
if not errorlevel 1 (
  echo ERROR: PlantState must not own a per-instance lookup dictionary.
  exit /b 1
)

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Plant.PlantCompositionTests.IndexedLookups_PreserveCanonicalIdentityUnknownIdAndStateOrdering" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.MainSteam.CompressibleSteamFlowDefinitionTests.CriticalPressureRatio_RemainsCanonicalForImmutableDefinition" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.M10961ChallengeLifecycleContractTests.LifecycleChanged_PreservesPerStepObservationChangeSemanticsWithoutStringFingerprint" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests.Current_DescribesM10972Hotfix2Rev1MeasuredTenMillisecondHotPathHardeningCandidate" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Milestones.M10972Hotfix2TenMillisecondHotPathHardeningTests.MeasuredHotPathAudit_EliminatesLookupAndObservationFingerprintAllocationsWithoutChangingSemantics" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.2 Hotfix 2 REV1 artifact summary ===
if exist "artifacts\m10972-hotfix2-ten-ms-hot-path\01-m10972-hotfix2-ten-millisecond-hot-path-hardening.summary.txt" (
  type "artifacts\m10972-hotfix2-ten-ms-hot-path\01-m10972-hotfix2-ten-millisecond-hot-path-hardening.summary.txt"
) else (
  echo ERROR: expected M10.9.7.2 Hotfix 2 REV1 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.2 Hotfix 2 REV1 measured hot-path audit completed.
echo If build, ordinary tests and this focused gate are green, validate Hotfix 2 REV1 before M10.9.7.3 live Mission/Performance wiring.
exit /b 0
