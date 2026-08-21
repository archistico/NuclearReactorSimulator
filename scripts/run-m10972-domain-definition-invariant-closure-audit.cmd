@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "DOMAIN_PROJECT=tests\NuclearReactorSimulator.Domain.Tests\NuclearReactorSimulator.Domain.Tests.csproj"
set "SIM_PROJECT=tests\NuclearReactorSimulator.Simulation.Tests\NuclearReactorSimulator.Simulation.Tests.csproj"

echo Running M10.9.7.2 Hotfix 1 REV1 Domain Definition Invariant Closure audit...
echo.
echo This gate closes construction-time Domain gaps found before M10.9.7.3 live workstation wiring.
echo It adds fail-closed definition guards and canonical-reference checks only; no solver retuning, UI activation,
echo scoring arithmetic, challenge definition, protection authority or plant command authority is introduced.
echo.

if exist "artifacts\m10972-hotfix1-domain-invariant-closure" rd /s /q "artifacts\m10972-hotfix1-domain-invariant-closure"

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Physics.Electrical.ElectricalQuantityTests.SynchronousGeneratorDefinition_RejectsZeroOrDegenerateSynchronizationWindows" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%SIM_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Simulation.Tests.Physics.Electrical.GeneratorGridSolverTests.Definition_RejectsSynchronizationWindowsThatSpanNominalGridEnvelope" ^
  --parallel none
if errorlevel 1 exit /b 1

set "APPLICATION_PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

dotnet test --project "%APPLICATION_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.ApplicationDescriptorTests.Current_DescribesM10972Hotfix1Rev1DomainDefinitionInvariantClosureCandidate" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Physics.Reactor.PrimaryCircuit.SteamDrums.SteamDrumSystemDefinitionTests.SteamSourceDefinition_RejectsDefaultHydraulicResistance" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Physics.Reactor.IodineXenon.IodineXenonDomainTests.Definition_RejectsDefaultDecayConstantsAtConstructionBoundary" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Physics.TurbineIsland.Turbine.TurbineExpansionSystemDefinitionTests.StageDefinition_RejectsDefaultExpansionResistanceWhenSpecified" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Physics.Control.ControlDefinitionTests.RodActuator_RejectsUnknownCommandTargetKind" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Plant.PlantCompositionTests.State_RejectsStructurallyEqualButNonCanonicalDefinitions" ^
  --parallel none
if errorlevel 1 exit /b 1

dotnet test --project "%DOMAIN_PROJECT%" --no-build -- ^
  --filter-method "NuclearReactorSimulator.Domain.Tests.Milestones.M10972Hotfix1DomainDefinitionInvariantClosureTests.ArtifactSummary_WritesM10972Hotfix1DomainInvariantEvidence" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.7.2 Hotfix 1 REV1 artifact summary ===
if exist "artifacts\m10972-hotfix1-domain-invariant-closure\01-m10972-hotfix1-domain-definition-invariant-closure.summary.txt" (
  type "artifacts\m10972-hotfix1-domain-invariant-closure\01-m10972-hotfix1-domain-definition-invariant-closure.summary.txt"
) else (
  echo ERROR: expected M10.9.7.2 Hotfix 1 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.7.2 Hotfix 1 REV1 Domain Definition Invariant Closure audit completed.
echo If build, ordinary tests and this focused gate are green, validate Hotfix 1 before measured pre-live 10-ms hot-path hardening.
exit /b 0
