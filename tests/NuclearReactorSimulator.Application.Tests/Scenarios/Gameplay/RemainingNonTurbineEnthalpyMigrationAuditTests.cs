using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Boundaries;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>M10.9.4.1-G.3 remaining non-turbine enthalpy-migration evidence.</summary>
public sealed class RemainingNonTurbineEnthalpyMigrationAuditTests
{
    [Fact]
    public void CurrentV2_DefinitionsMigrateEveryRemainingNonTurbineTransportOwner()
    {
        foreach (var engine in CurrentV2Engines())
        {
            var definition = engine.CurrentState.PlantDefinition;
            var condenserSystem = definition.CondensateFeedwaterSystem.CondenserSystem;
            var mainSteam = condenserSystem.TurbineExpansionSystem.MainSteamNetwork;
            var boundaries = mainSteam.PrimaryCircuit.BoundarySystem;

            Assert.NotEmpty(definition.PlantDefinition.Pumps);
            Assert.All(definition.PlantDefinition.Pumps, static pump =>
                Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, pump.Pipe.EnergyTransportMode));
            Assert.All(boundaries.SteamDrumSystem.Drums, static drum =>
                Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, drum.EnergyTransportMode));
            Assert.All(boundaries.FeedwaterBoundaries, static boundary =>
                Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, boundary.EnergyTransportMode));
            Assert.All(boundaries.SteamExportBoundaries, static boundary =>
                Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, boundary.EnergyTransportMode));
            Assert.All(mainSteam.TurbineAdmissionBoundaries, static boundary =>
                Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, boundary.EnergyTransportMode));
            Assert.All(mainSteam.ReliefBoundaries, static boundary =>
                Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, boundary.EnergyTransportMode));
            Assert.All(condenserSystem.Condensers, static condenser =>
                Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, condenser.EnergyTransportMode));
            Assert.All(condenserSystem.TurbineBypasses, static bypass =>
                Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, bypass.EnergyTransportMode));
        }
    }

    [Fact]
    public void LegacyColdShutdown_PreservesInternalEnergyForEveryRemainingOwner()
    {
        var engine = CreateEngine(new ColdShutdownInitialConditionFactory());
        var definition = engine.CurrentState.PlantDefinition;
        var condenserSystem = definition.CondensateFeedwaterSystem.CondenserSystem;
        var mainSteam = condenserSystem.TurbineExpansionSystem.MainSteamNetwork;
        var boundaries = mainSteam.PrimaryCircuit.BoundarySystem;

        Assert.All(definition.PlantDefinition.Pumps, static pump =>
            Assert.Equal(FluidEnergyTransportMode.SpecificInternalEnergy, pump.Pipe.EnergyTransportMode));
        Assert.All(boundaries.SteamDrumSystem.Drums, static drum =>
            Assert.Equal(FluidEnergyTransportMode.SpecificInternalEnergy, drum.EnergyTransportMode));
        Assert.All(boundaries.FeedwaterBoundaries, static boundary =>
            Assert.Equal(FluidEnergyTransportMode.SpecificInternalEnergy, boundary.EnergyTransportMode));
        Assert.All(boundaries.SteamExportBoundaries, static boundary =>
            Assert.Equal(FluidEnergyTransportMode.SpecificInternalEnergy, boundary.EnergyTransportMode));
        Assert.All(mainSteam.TurbineAdmissionBoundaries, static boundary =>
            Assert.Equal(FluidEnergyTransportMode.SpecificInternalEnergy, boundary.EnergyTransportMode));
        Assert.All(condenserSystem.Condensers, static condenser =>
            Assert.Equal(FluidEnergyTransportMode.SpecificInternalEnergy, condenser.EnergyTransportMode));
        Assert.Empty(mainSteam.ReliefBoundaries);
        Assert.Empty(condenserSystem.TurbineBypasses);
    }

    [Fact]
    public void CurrentV2SteamDrum_AdvectsEnthalpyAndClosesInternalSeparationExactly()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var drum = Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl
            .FullPlant.IntegratedCycle.PrimaryCircuit.SteamDrums.Drums);

        Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, drum.EnergyTransportMode);
        Assert.Equal(
            drum.SteamSpecificEnthalpy.JoulesPerKilogram,
            drum.SteamSpecificInternalEnergy.JoulesPerKilogram + drum.SteamSpecificFlowWork.JoulesPerKilogram,
            6);
        Assert.Equal(
            drum.LiquidSpecificEnthalpy.JoulesPerKilogram,
            drum.LiquidSpecificInternalEnergy.JoulesPerKilogram + drum.LiquidSpecificFlowWork.JoulesPerKilogram,
            6);
        Assert.Equal(drum.SteamSpecificEnthalpy, drum.SteamAdvectedSpecificEnergy);
        Assert.Equal(drum.LiquidSpecificEnthalpy, drum.LiquidAdvectedSpecificEnergy);
        Assert.Equal(
            drum.SteamEnergyRate.Watts,
            drum.SteamInternalEnergyRate.Watts + drum.SteamFlowWorkRate.Watts,
            3);
        Assert.Equal(
            drum.LiquidEnergyRate.Watts,
            drum.LiquidInternalEnergyRate.Watts + drum.LiquidFlowWorkRate.Watts,
            3);
        Assert.Equal(0d, drum.SeparationMassResidualKilogramsPerSecond, 12);
        Assert.Equal(0d, drum.SeparationEnergyResidualWatts, 6);
    }

    [Fact]
    public void CurrentV2Condenser_UsesEnthalpyDropAndKeepsHeatRejectionSingleCounted()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var condenser = Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl
            .FullPlant.IntegratedCycle.Condenser.Condensers);

        Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, condenser.EnergyTransportMode);
        Assert.Equal(condenser.SteamSpecificEnthalpy, condenser.SteamAdvectedSpecificEnergy);
        Assert.Equal(condenser.CondensateSpecificEnthalpy, condenser.CondensateAdvectedSpecificEnergy);
        Assert.Equal(
            condenser.SteamEnergyRemovalRate.Watts,
            condenser.SteamInternalEnergyRemovalRate.Watts + condenser.SteamFlowWorkRemovalRate.Watts,
            3);
        Assert.Equal(
            condenser.HotwellEnergyAdditionRate.Watts,
            condenser.HotwellInternalEnergyAdditionRate.Watts + condenser.HotwellFlowWorkAdditionRate.Watts,
            3);
        Assert.Equal(
            condenser.HeatRejectionPower.Watts,
            condenser.SteamEnergyRemovalRate.Watts - condenser.HotwellEnergyAdditionRate.Watts,
            3);
        Assert.True(condenser.SpecificCondensationEnergyDrop >= SpecificEnergy.Zero);
    }

    [Fact]
    public void CurrentV2PrimaryBoundaries_ApplyEnthalpyAndDeclareMatchingExternalExchange()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var plant = engine.CurrentState.PlantState.PlantState;
        var mainSteam = engine.CurrentState.PlantDefinition.CondensateFeedwaterSystem.CondenserSystem
            .TurbineExpansionSystem.MainSteamNetwork;
        var definition = mainSteam.PrimaryCircuit.BoundarySystem;
        var feedwaterState = plant.GetFluidNode("feedwater-inventory");
        var feedwaterEnthalpy = FluidEnergyTransport.ResolveSpecificEnthalpy(
            feedwaterState.SpecificInternalEnergy,
            feedwaterState.Pressure,
            feedwaterState.Density);
        var flow = MassFlowRate.FromKilogramsPerSecond(1d);
        var inputs = new PrimaryCircuitBoundaryInputs(
            definition,
            new[]
            {
                new FeedwaterBoundaryInput(
                    Assert.Single(definition.FeedwaterBoundaries).Id,
                    flow,
                    feedwaterState.SpecificInternalEnergy,
                    feedwaterEnthalpy),
            },
            new[]
            {
                new SteamExportBoundaryInput(Assert.Single(definition.SteamExportBoundaries).Id, flow),
            });

        var result = new PrimaryCircuitBoundarySolver(definition).Solve(plant, inputs);
        var feedwater = Assert.Single(result.Snapshot.FeedwaterBoundaries);
        var export = Assert.Single(result.Snapshot.SteamExportBoundaries);

        Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, feedwater.EnergyTransportMode);
        Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, export.EnergyTransportMode);
        Assert.Equal(
            feedwater.SpecificEnthalpy.JoulesPerKilogram,
            feedwater.SpecificInternalEnergy.JoulesPerKilogram + feedwater.SpecificFlowWork.JoulesPerKilogram,
            6);
        Assert.Equal(
            export.ExportedSpecificEnthalpy.JoulesPerKilogram,
            export.ExportedSpecificInternalEnergy.JoulesPerKilogram + export.ExportedSpecificFlowWork.JoulesPerKilogram,
            6);
        Assert.Equal(feedwater.EnergyInputRate.Watts, feedwater.InternalEnergyInputRate.Watts + feedwater.FlowWorkInputRate.Watts, 3);
        Assert.Equal(export.EnergyExportRate.Watts, export.InternalEnergyExportRate.Watts + export.FlowWorkExportRate.Watts, 3);
        Assert.Equal(0d, result.SourceTerms.ExternalMassFlowRate.KilogramsPerSecond, 12);
        Assert.Equal(
            result.SourceTerms.ExternalPower.Watts,
            feedwater.EnergyInputRate.Watts - export.EnergyExportRate.Watts,
            3);
    }

    [Fact]
    public void CurrentV2FeedwaterBoundary_RequiresExplicitEnthalpyForPositiveFlow()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var plant = engine.CurrentState.PlantState.PlantState;
        var definition = engine.CurrentState.PlantDefinition.CondensateFeedwaterSystem.CondenserSystem
            .TurbineExpansionSystem.MainSteamNetwork.PrimaryCircuit.BoundarySystem;
        var state = plant.GetFluidNode("feedwater-inventory");
        var inputs = new PrimaryCircuitBoundaryInputs(
            definition,
            new[]
            {
                new FeedwaterBoundaryInput(
                    Assert.Single(definition.FeedwaterBoundaries).Id,
                    MassFlowRate.FromKilogramsPerSecond(1d),
                    state.SpecificInternalEnergy),
            },
            new[]
            {
                new SteamExportBoundaryInput(Assert.Single(definition.SteamExportBoundaries).Id, MassFlowRate.Zero),
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => new PrimaryCircuitBoundarySolver(definition).Solve(plant, inputs));
        Assert.Contains("requires explicit incoming specific enthalpy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CurrentV2ReliefAndBypass_UseEnthalpyWhilePreservingBoundaryOwnership()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var committed = engine.CurrentState.PlantState.PlantState;
        var condenserSystem = engine.CurrentState.PlantDefinition.CondensateFeedwaterSystem.CondenserSystem;
        var mainSteam = condenserSystem.TurbineExpansionSystem.MainSteamNetwork;
        var forced = WithPressures(committed, 6.8d, committed.GetFluidNode("exhaust").Pressure.Megapascals);

        var reliefResult = new MainSteamReliefBoundarySolver(mainSteam).Solve(forced);
        var relief = Assert.Single(reliefResult.Snapshots);
        Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, relief.EnergyTransportMode);
        Assert.Equal(relief.EnergyExportRate.Watts, relief.InternalEnergyExportRate.Watts + relief.FlowWorkExportRate.Watts, 3);
        Assert.Equal(-relief.MassFlowRate.KilogramsPerSecond, reliefResult.SourceTerms.ExternalMassFlowRate.KilogramsPerSecond, 10);
        Assert.Equal(-relief.EnergyExportRate.Watts, reliefResult.SourceTerms.ExternalPower.Watts, 3);

        var bypassResult = new TurbineBypassSolver(condenserSystem).Solve(forced);
        var bypass = Assert.Single(bypassResult.Snapshots);
        Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, bypass.EnergyTransportMode);
        Assert.Equal(bypass.AdvectedEnergyTransferRate.Watts, bypass.InternalEnergyTransferRate.Watts + bypass.FlowWorkTransferRate.Watts, 3);
        Assert.Equal(MassFlowRate.Zero, bypassResult.SourceTerms.ExternalMassFlowRate);
        Assert.Equal(Power.Zero, bypassResult.SourceTerms.ExternalPower);
        var source = bypassResult.SourceTerms.FluidNodeBalances[bypass.SourceHeaderNodeId];
        var destination = bypassResult.SourceTerms.FluidNodeBalances[bypass.DestinationSteamSpaceNodeId];
        Assert.Equal(0d, source.NetMassFlowRate.KilogramsPerSecond + destination.NetMassFlowRate.KilogramsPerSecond, 10);
        Assert.Equal(0d, source.NetEnergyRate.Watts + destination.NetEnergyRate.Watts, 3);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "RemainingNonTurbineEnthalpyMigrationAudit")]
    public void CurrentV2RemainingNonTurbineOwners_RecordEnthalpyAndSingleCountedWork()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var rows = BuildAuditRows(engine);
        Assert.All(rows, static row =>
        {
            Assert.Equal(FluidEnergyTransportMode.SpecificEnthalpy, row.EnergyTransportMode);
            Assert.InRange(Math.Abs(row.OwnershipResidualWatts), 0d, 0.001d);
        });
        Assert.Contains(rows, static row => Math.Abs(row.FlowWorkRateMegawatts) > 0.001d);
        WriteAuditReports(rows);
    }

    private static IReadOnlyList<IntegratedAutomaticOperationRuntimeEngine> CurrentV2Engines()
        => new[]
        {
            CreateEngine(new DesktopSustainedGenerationInitialConditionFactory()),
            CreateEngine(new GridSynchronizationSustainedInitialConditionFactory()),
        };

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(IVersionedInitialConditionFactory factory)
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());

    private static IReadOnlyList<AuditRow> BuildAuditRows(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var rows = new List<AuditRow>();
        var canonical = engine.LatestCanonicalSnapshot.Control.ProtectedControl.FullPlant.IntegratedCycle;
        var plant = engine.CurrentState.PlantState.PlantState;
        var definition = engine.CurrentState.PlantDefinition;

        foreach (var pump in definition.PlantDefinition.Pumps.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var result = new PumpFlowSolver().Solve(
                pump,
                plant.GetPump(pump.Id),
                plant.GetFluidNode(pump.Pipe.FromNodeId),
                plant.GetFluidNode(pump.Pipe.ToNodeId));
            var combined = result.FromNodeBalance + result.ToNodeBalance;
            rows.Add(new AuditRow(
                "pump",
                pump.Id,
                result.EnergyTransportMode,
                result.MassFlowRate.KilogramsPerSecond,
                result.InternalEnergyFlowRate.Megawatts,
                result.FlowWorkRate.Megawatts,
                result.AdvectedEnergyFlowRate.Megawatts,
                combined.NetEnergyRate.Watts - result.HydraulicPowerExchange.Watts,
                0d));
        }

        var drum = Assert.Single(canonical.PrimaryCircuit.SteamDrums.Drums);
        rows.Add(new AuditRow(
            "steam-drum-steam",
            drum.DrumId,
            drum.EnergyTransportMode,
            drum.SeparatedSteamMassFlowRate.KilogramsPerSecond,
            drum.SteamInternalEnergyRate.Megawatts,
            drum.SteamFlowWorkRate.Megawatts,
            drum.SteamEnergyRate.Megawatts,
            drum.SeparationEnergyResidualWatts,
            0d));
        rows.Add(new AuditRow(
            "steam-drum-liquid",
            drum.DrumId,
            drum.EnergyTransportMode,
            drum.RecirculatedLiquidMassFlowRate.KilogramsPerSecond,
            drum.LiquidInternalEnergyRate.Megawatts,
            drum.LiquidFlowWorkRate.Megawatts,
            drum.LiquidEnergyRate.Megawatts,
            drum.SeparationEnergyResidualWatts,
            0d));

        var feedwaterBoundary = Assert.Single(canonical.PrimaryCircuit.Boundaries.FeedwaterBoundaries);
        rows.Add(new AuditRow(
            "external-feedwater",
            feedwaterBoundary.BoundaryId,
            feedwaterBoundary.EnergyTransportMode,
            feedwaterBoundary.MassFlowRate.KilogramsPerSecond,
            feedwaterBoundary.InternalEnergyInputRate.Megawatts,
            feedwaterBoundary.FlowWorkInputRate.Megawatts,
            feedwaterBoundary.EnergyInputRate.Megawatts,
            feedwaterBoundary.EnergyInputRate.Watts
                - feedwaterBoundary.InternalEnergyInputRate.Watts
                - feedwaterBoundary.FlowWorkInputRate.Watts,
            feedwaterBoundary.EnergyInputRate.Megawatts));
        var steamExportBoundary = Assert.Single(canonical.PrimaryCircuit.Boundaries.SteamExportBoundaries);
        rows.Add(new AuditRow(
            "external-steam-export",
            steamExportBoundary.BoundaryId,
            steamExportBoundary.EnergyTransportMode,
            steamExportBoundary.MassFlowRate.KilogramsPerSecond,
            steamExportBoundary.InternalEnergyExportRate.Megawatts,
            steamExportBoundary.FlowWorkExportRate.Megawatts,
            steamExportBoundary.EnergyExportRate.Megawatts,
            steamExportBoundary.EnergyExportRate.Watts
                - steamExportBoundary.InternalEnergyExportRate.Watts
                - steamExportBoundary.FlowWorkExportRate.Watts,
            -steamExportBoundary.EnergyExportRate.Megawatts));
        var admissionBoundary = Assert.Single(canonical.TurbineExpansion.MainSteamNetwork.TurbineAdmissionBoundaries);
        rows.Add(new AuditRow(
            "external-turbine-admission",
            admissionBoundary.BoundaryId,
            admissionBoundary.EnergyTransportMode,
            admissionBoundary.MassFlowRate.KilogramsPerSecond,
            admissionBoundary.InternalEnergyExportRate.Megawatts,
            admissionBoundary.FlowWorkExportRate.Megawatts,
            admissionBoundary.EnergyExportRate.Megawatts,
            admissionBoundary.EnergyExportRate.Watts
                - admissionBoundary.InternalEnergyExportRate.Watts
                - admissionBoundary.FlowWorkExportRate.Watts,
            -admissionBoundary.EnergyExportRate.Megawatts));

        var condenser = Assert.Single(canonical.Condenser.Condensers);
        rows.Add(new AuditRow(
            "condenser-steam",
            condenser.CondenserId,
            condenser.EnergyTransportMode,
            condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
            condenser.SteamInternalEnergyRemovalRate.Megawatts,
            condenser.SteamFlowWorkRemovalRate.Megawatts,
            condenser.SteamEnergyRemovalRate.Megawatts,
            condenser.SteamEnergyRemovalRate.Watts
                - condenser.SteamInternalEnergyRemovalRate.Watts
                - condenser.SteamFlowWorkRemovalRate.Watts,
            -condenser.HeatRejectionPower.Megawatts));
        rows.Add(new AuditRow(
            "condenser-hotwell",
            condenser.CondenserId,
            condenser.EnergyTransportMode,
            condenser.ActualCondensationMassFlowRate.KilogramsPerSecond,
            condenser.HotwellInternalEnergyAdditionRate.Megawatts,
            condenser.HotwellFlowWorkAdditionRate.Megawatts,
            condenser.HotwellEnergyAdditionRate.Megawatts,
            condenser.HotwellEnergyAdditionRate.Watts
                - condenser.HotwellInternalEnergyAdditionRate.Watts
                - condenser.HotwellFlowWorkAdditionRate.Watts,
            0d));

        var condenserSystem = definition.CondensateFeedwaterSystem.CondenserSystem;
        var mainSteam = condenserSystem.TurbineExpansionSystem.MainSteamNetwork;
        var forced = WithPressures(plant, 6.8d, plant.GetFluidNode("exhaust").Pressure.Megapascals);
        var relief = Assert.Single(new MainSteamReliefBoundarySolver(mainSteam).Solve(forced).Snapshots);
        rows.Add(new AuditRow(
            "external-relief",
            relief.BoundaryId,
            relief.EnergyTransportMode,
            relief.MassFlowRate.KilogramsPerSecond,
            relief.InternalEnergyExportRate.Megawatts,
            relief.FlowWorkExportRate.Megawatts,
            relief.EnergyExportRate.Megawatts,
            relief.EnergyExportRate.Watts - relief.InternalEnergyExportRate.Watts - relief.FlowWorkExportRate.Watts,
            -relief.EnergyExportRate.Megawatts));
        var bypass = Assert.Single(new TurbineBypassSolver(condenserSystem).Solve(forced).Snapshots);
        rows.Add(new AuditRow(
            "internal-bypass",
            bypass.BypassId,
            bypass.EnergyTransportMode,
            bypass.MassFlowRate.KilogramsPerSecond,
            bypass.InternalEnergyTransferRate.Megawatts,
            bypass.FlowWorkTransferRate.Megawatts,
            bypass.AdvectedEnergyTransferRate.Megawatts,
            bypass.AdvectedEnergyTransferRate.Watts - bypass.InternalEnergyTransferRate.Watts - bypass.FlowWorkTransferRate.Watts,
            0d));

        return rows;
    }

    private static PlantState WithPressures(
        PlantState source,
        double headerPressureMegapascals,
        double exhaustPressureMegapascals)
    {
        var fluidNodes = source.FluidNodes.Select(node =>
        {
            var pressure = node.Id switch
            {
                "header" => Pressure.FromMegapascals(headerPressureMegapascals),
                "exhaust" => Pressure.FromMegapascals(exhaustPressureMegapascals),
                _ => node.Pressure,
            };
            return pressure == node.Pressure
                ? node
                : new FluidNodeState(
                    node.Definition,
                    node.Inventory,
                    new FluidThermodynamicState(pressure, node.Temperature, node.Phase, node.VaporQuality));
        });

        return new PlantState(
            source.Definition,
            fluidNodes,
            source.Valves,
            source.Pumps,
            source.ThermalBodies,
            source.HeatSources);
    }

    private static void WriteAuditReports(IReadOnlyList<AuditRow> rows)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "g3-remaining-non-turbine-enthalpy");
        Directory.CreateDirectory(directory);
        const string stem = "01-current-v2-remaining-non-turbine-enthalpy";
        var csv = new StringBuilder();
        csv.AppendLine("component_kind,component_id,energy_transport_mode,mass_flow_kg_per_s,internal_energy_rate_mw,flow_work_rate_mw,advected_energy_rate_mw,ownership_residual_w,declared_external_power_mw");
        foreach (var row in rows)
        {
            csv.AppendLine(FormattableString.Invariant(
                $"{row.ComponentKind},{row.ComponentId},{row.EnergyTransportMode},{row.MassFlowKilogramsPerSecond:0.000000000},{row.InternalEnergyRateMegawatts:0.000000000},{row.FlowWorkRateMegawatts:0.000000000},{row.AdvectedEnergyRateMegawatts:0.000000000},{row.OwnershipResidualWatts:0.000000},{row.DeclaredExternalPowerMegawatts:0.000000000}"));
        }
        File.WriteAllText(Path.Combine(directory, $"{stem}.csv"), csv.ToString(), new UTF8Encoding(false));

        var maximumFlowWork = rows.Max(static row => Math.Abs(row.FlowWorkRateMegawatts));
        var totalFlowWork = rows.Sum(static row => Math.Abs(row.FlowWorkRateMegawatts));
        var maximumResidual = rows.Max(static row => Math.Abs(row.OwnershipResidualWatts));
        var externalOwners = rows.Count(static row => row.DeclaredExternalPowerMegawatts != 0d);
        var summary = string.Join(Environment.NewLine,
            $"=== {stem} ===",
            "Current-v2 pump paths, steam-drum separation, active boundaries, condenser phase change, relief and bypass apply h*m_dot while turbine expansion remains reserved for G.4.",
            FormattableString.Invariant(
                $"samples={rows.Count}; pump-paths={rows.Count(static row => row.ComponentKind == "pump")}; drum-paths={rows.Count(static row => row.ComponentKind.StartsWith("steam-drum", StringComparison.Ordinal))}; condenser-paths={rows.Count(static row => row.ComponentKind.StartsWith("condenser", StringComparison.Ordinal))}; external-boundary-owners={externalOwners};"),
            FormattableString.Invariant(
                $"maximum-absolute-flow-work-rate={maximumFlowWork:0.000000000} MW; total-absolute-flow-work-rate={totalFlowWork:0.000000000} MW; maximum-ownership-residual={maximumResidual:0.000000} W;"),
            "runtime-remaining-migration-active=True; node-inventories-remain-internal-energy=True; pump-hydraulic-work-single-count=True; condenser-heat-rejection-single-count=True; relief-external=True; bypass-internal=True; turbine-expansion-migration-active=False",
            string.Empty);
        File.WriteAllText(Path.Combine(directory, $"{stem}.summary.txt"), summary, new UTF8Encoding(false));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NuclearReactorSimulator.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate NuclearReactorSimulator.sln from the test base directory.");
    }

    private sealed record AuditRow(
        string ComponentKind,
        string ComponentId,
        FluidEnergyTransportMode EnergyTransportMode,
        double MassFlowKilogramsPerSecond,
        double InternalEnergyRateMegawatts,
        double FlowWorkRateMegawatts,
        double AdvectedEnergyRateMegawatts,
        double OwnershipResidualWatts,
        double DeclaredExternalPowerMegawatts);
}
