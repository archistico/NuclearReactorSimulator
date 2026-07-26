using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>M10.9.4.1-F.3 pressure-actuated header-to-condenser turbine-bypass regressions.</summary>
public sealed class TurbineBypassImplementationTests
{
    [Fact]
    public void CurrentV2SustainedProfilesOwnSingleTurbineBypassWhileLegacyRemainsUnchanged()
    {
        var desktop = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var synchronization = CreateEngine(new GridSynchronizationSustainedInitialConditionFactory());
        var legacy = CreateEngine(new ColdShutdownInitialConditionFactory());

        AssertBypassContract(desktop);
        AssertBypassContract(synchronization);
        Assert.Empty(legacy.CurrentState.PlantDefinition.CondensateFeedwaterSystem.CondenserSystem.TurbineBypasses);
    }

    [Fact]
    public void CurrentV2OperationalSeedsRemainBelowBypassSetPressureAndPublishClosedSnapshot()
    {
        foreach (var engine in new[]
                 {
                     CreateEngine(new DesktopSustainedGenerationInitialConditionFactory()),
                     CreateEngine(new GridSynchronizationSustainedInitialConditionFactory()),
                 })
        {
            var bypass = Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl
                .FullPlant.IntegratedCycle.Condenser.TurbineBypasses);

            Assert.True(bypass.SourcePressure < Pressure.FromMegapascals(6.4d));
            Assert.Equal(0d, bypass.OpenFraction, 12);
            Assert.Equal(MassFlowRate.Zero, bypass.MassFlowRate);
            Assert.Equal(Power.Zero, bypass.InternalEnergyTransferRate);
            Assert.True(bypass.DestinationPressure < bypass.SourcePressure);
        }
    }

    [Fact]
    public void CurrentV2TurbineBypassDestinationIsTheCanonicalCondenserSteamSpace()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var condensers = engine.CurrentState.PlantDefinition.CondensateFeedwaterSystem.CondenserSystem;
        var bypass = Assert.Single(condensers.TurbineBypasses);
        var condenser = condensers.GetCondenser(bypass.CondenserId);
        var snapshot = Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl
            .FullPlant.IntegratedCycle.Condenser.TurbineBypasses);

        Assert.Equal(condenser.SteamSpaceNodeId, snapshot.DestinationSteamSpaceNodeId);
        Assert.Equal("exhaust", snapshot.DestinationSteamSpaceNodeId);
        Assert.Equal("header", snapshot.SourceHeaderNodeId);
    }

    [Fact(Explicit = true)]
    [Trait("Category", "TurbineBypassImplementationAudit")]
    public void CurrentV2PressureAndBackpressureSweeps_RecordCapacityAndConservativeInternalTransfer()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var definition = engine.CurrentState.PlantDefinition.CondensateFeedwaterSystem.CondenserSystem;
        var solver = new TurbineBypassSolver(definition);
        var committedPlant = engine.CurrentState.PlantState.PlantState;
        var initialExhaustPressure = committedPlant.GetFluidNode("exhaust").Pressure;
        var pressureRows = new List<PressureSweepRow>();

        for (var index = 0; index <= 40; index++)
        {
            var headerPressureMegapascals = 6.2d + (index * 0.01d);
            var state = WithPressures(committedPlant, headerPressureMegapascals, initialExhaustPressure.Megapascals);
            var result = solver.Solve(state);
            var snapshot = Assert.Single(result.Snapshots);
            AssertInternalTransfer(result, snapshot);

            pressureRows.Add(new PressureSweepRow(
                headerPressureMegapascals,
                snapshot.DestinationPressure.Megapascals,
                snapshot.OpenFraction,
                snapshot.VaporAvailabilityFraction,
                snapshot.EffectiveThroatArea.SquareMillimetres,
                snapshot.IsChoked,
                snapshot.MassFlowRate.KilogramsPerSecond,
                snapshot.InternalEnergyTransferRate.Megawatts));
        }

        Assert.All(pressureRows.Where(static row => row.HeaderPressureMegapascals <= 6.4d),
            static row => Assert.Equal(0d, row.MassFlowKilogramsPerSecond, 12));
        Assert.All(pressureRows.Where(static row => row.HeaderPressureMegapascals >= 6.5d),
            static row => Assert.Equal(1d, row.OpenFraction, 12));
        Assert.True(pressureRows.Zip(pressureRows.Skip(1), static (left, right) =>
            right.MassFlowKilogramsPerSecond >= left.MassFlowKilogramsPerSecond - 1e-10d).All(static value => value));
        Assert.True(pressureRows[^1].MassFlowKilogramsPerSecond > 12d);
        Assert.True(pressureRows.Where(static row => row.MassFlowKilogramsPerSecond > 0d).All(static row => row.IsChoked));

        const double fullOpenSourcePressureMegapascals = 6.5d;
        var backpressureRows = new List<BackpressureSweepRow>();
        for (var index = 1; index <= 100; index++)
        {
            var pressureRatio = index / 100d;
            var destinationPressureMegapascals = fullOpenSourcePressureMegapascals * pressureRatio;
            var state = WithPressures(committedPlant, fullOpenSourcePressureMegapascals, destinationPressureMegapascals);
            var result = solver.Solve(state);
            var snapshot = Assert.Single(result.Snapshots);
            AssertInternalTransfer(result, snapshot);

            backpressureRows.Add(new BackpressureSweepRow(
                pressureRatio,
                destinationPressureMegapascals,
                snapshot.IsChoked,
                snapshot.MassFlowRate.KilogramsPerSecond,
                snapshot.InternalEnergyTransferRate.Megawatts));
        }

        var heatCapacityRatio = Assert.Single(definition.TurbineBypasses).FlowDefinition.HeatCapacityRatio;
        var analyticCriticalRatio = Math.Pow(2d / (heatCapacityRatio + 1d), heatCapacityRatio / (heatCapacityRatio - 1d));
        var plateau = backpressureRows[0].MassFlowKilogramsPerSecond;
        Assert.All(backpressureRows.Where(row => row.PressureRatio <= analyticCriticalRatio), row =>
        {
            Assert.True(row.IsChoked);
            Assert.Equal(plateau, row.MassFlowKilogramsPerSecond, 9);
        });
        Assert.True(backpressureRows.Zip(backpressureRows.Skip(1), static (left, right) =>
            right.MassFlowKilogramsPerSecond <= left.MassFlowKilogramsPerSecond + 1e-10d).All(static value => value));
        Assert.Equal(0d, backpressureRows[^1].MassFlowKilogramsPerSecond, 12);
        Assert.False(backpressureRows[^1].IsChoked);

        WriteAuditReports(pressureRows, backpressureRows, analyticCriticalRatio, initialExhaustPressure);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(IVersionedInitialConditionFactory factory)
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());

    private static void AssertBypassContract(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var bypass = Assert.Single(engine.CurrentState.PlantDefinition.CondensateFeedwaterSystem.CondenserSystem.TurbineBypasses);
        Assert.Equal("turbine-bypass", bypass.Id);
        Assert.Equal("header", bypass.SourceHeaderNodeId);
        Assert.Equal("condenser", bypass.CondenserId);
        Assert.Equal(6.4d, bypass.SetPressure.Megapascals, 12);
        Assert.Equal(6.5d, bypass.FullOpenPressure.Megapascals, 12);
        Assert.Equal(1_600d, bypass.FlowDefinition.FullOpenThroatArea.SquareMillimetres, 12);
    }

    private static void AssertInternalTransfer(TurbineBypassStepResult result, TurbineBypassSnapshot snapshot)
    {
        Assert.Equal(2, result.SourceTerms.FluidNodeBalances.Count);
        var source = result.SourceTerms.FluidNodeBalances[snapshot.SourceHeaderNodeId];
        var destination = result.SourceTerms.FluidNodeBalances[snapshot.DestinationSteamSpaceNodeId];
        Assert.Equal(-snapshot.MassFlowRate.KilogramsPerSecond, source.NetMassFlowRate.KilogramsPerSecond, 10);
        Assert.Equal(snapshot.MassFlowRate.KilogramsPerSecond, destination.NetMassFlowRate.KilogramsPerSecond, 10);
        Assert.Equal(-snapshot.InternalEnergyTransferRate.Watts, source.NetEnergyRate.Watts, 3);
        Assert.Equal(snapshot.InternalEnergyTransferRate.Watts, destination.NetEnergyRate.Watts, 3);
        Assert.Equal(0d, source.NetMassFlowRate.KilogramsPerSecond + destination.NetMassFlowRate.KilogramsPerSecond, 10);
        Assert.Equal(0d, source.NetEnergyRate.Watts + destination.NetEnergyRate.Watts, 3);
        Assert.Equal(MassFlowRate.Zero, result.SourceTerms.ExternalMassFlowRate);
        Assert.Equal(Power.Zero, result.SourceTerms.ExternalPower);
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

            if (pressure == node.Pressure)
            {
                return node;
            }

            return new FluidNodeState(
                node.Definition,
                node.Inventory,
                new FluidThermodynamicState(
                    pressure,
                    node.Temperature,
                    node.Phase,
                    node.VaporQuality));
        });

        return new PlantState(
            source.Definition,
            fluidNodes,
            source.Valves,
            source.Pumps,
            source.ThermalBodies,
            source.HeatSources);
    }

    private static void WriteAuditReports(
        IReadOnlyList<PressureSweepRow> pressureRows,
        IReadOnlyList<BackpressureSweepRow> backpressureRows,
        double analyticCriticalRatio,
        Pressure initialExhaustPressure)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "f3-turbine-bypass");
        Directory.CreateDirectory(directory);

        var pressureStem = "01-current-v2-turbine-bypass-source-pressure-sweep";
        var pressureCsv = new StringBuilder();
        pressureCsv.AppendLine("header_pressure_mpa,condenser_backpressure_mpa,open_fraction,vapor_availability_fraction,effective_throat_area_mm2,is_choked,mass_flow_kg_per_s,internal_energy_transfer_mw");
        foreach (var row in pressureRows)
        {
            pressureCsv.AppendLine(FormattableString.Invariant(
                $"{row.HeaderPressureMegapascals:0.000000},{row.CondenserBackpressureMegapascals:0.000000},{row.OpenFraction:0.000000},{row.VaporAvailabilityFraction:0.000000},{row.EffectiveThroatAreaSquareMillimetres:0.000000},{row.IsChoked},{row.MassFlowKilogramsPerSecond:0.000000000},{row.InternalEnergyTransferMegawatts:0.000000000}"));
        }
        File.WriteAllText(Path.Combine(directory, $"{pressureStem}.csv"), pressureCsv.ToString(), new UTF8Encoding(false));

        var firstOpen = pressureRows.First(static row => row.MassFlowKilogramsPerSecond > 0d);
        var firstFullOpen = pressureRows.First(static row => row.OpenFraction >= 1d);
        var maximum = pressureRows[^1];
        var pressureSummary = string.Join(Environment.NewLine,
            $"=== {pressureStem} ===",
            "Automatic header-to-condenser steam dump over the validated F.1 capacity seam; internal-energy transport remains the pre-Phase-G convention.",
            FormattableString.Invariant(
                $"samples={pressureRows.Count}; header-pressure={pressureRows[0].HeaderPressureMegapascals:0.000000}..{maximum.HeaderPressureMegapascals:0.000000} MPa; set-pressure=6.400000 MPa; full-open-pressure=6.500000 MPa; committed-condenser-backpressure={initialExhaustPressure.Megapascals:0.000000} MPa;"),
            FormattableString.Invariant(
                $"first-open-pressure={firstOpen.HeaderPressureMegapascals:0.000000} MPa; first-full-open-pressure={firstFullOpen.HeaderPressureMegapascals:0.000000} MPa; vapor-availability={maximum.VaporAvailabilityFraction:0.000000}; capacity-at-{maximum.HeaderPressureMegapascals:0.00}MPa={maximum.MassFlowKilogramsPerSecond:0.000000000} kg/s; internal-energy-transfer={maximum.InternalEnergyTransferMegawatts:0.000000000} MW;"),
            "mass-flow-monotonic=True; internal-transfer-conservative=True; external-boundary-exchange=False; atmospheric-relief-separate=True",
            string.Empty);
        File.WriteAllText(Path.Combine(directory, $"{pressureStem}.summary.txt"), pressureSummary, new UTF8Encoding(false));

        var backpressureStem = "02-current-v2-turbine-bypass-condenser-backpressure-sweep";
        var backpressureCsv = new StringBuilder();
        backpressureCsv.AppendLine("pressure_ratio,condenser_backpressure_mpa,is_choked,mass_flow_kg_per_s,internal_energy_transfer_mw");
        foreach (var row in backpressureRows)
        {
            backpressureCsv.AppendLine(FormattableString.Invariant(
                $"{row.PressureRatio:0.000000},{row.CondenserBackpressureMegapascals:0.000000},{row.IsChoked},{row.MassFlowKilogramsPerSecond:0.000000000},{row.InternalEnergyTransferMegawatts:0.000000000}"));
        }
        File.WriteAllText(Path.Combine(directory, $"{backpressureStem}.csv"), backpressureCsv.ToString(), new UTF8Encoding(false));

        var firstUnchoked = backpressureRows.First(static row => !row.IsChoked);
        var backpressureSummary = string.Join(Environment.NewLine,
            $"=== {backpressureStem} ===",
            "Full-open bypass capacity resolved against committed condenser steam-space pressure; no fixed receiver pressure or reverse-flow path is used.",
            FormattableString.Invariant(
                $"samples={backpressureRows.Count}; source-pressure=6.500000 MPa; pressure-ratio=0.010000..1.000000; analytic-critical-ratio={analyticCriticalRatio:0.000000}; first-sampled-unchoked-ratio={firstUnchoked.PressureRatio:0.000000};"),
            FormattableString.Invariant(
                $"choked-plateau-capacity={backpressureRows[0].MassFlowKilogramsPerSecond:0.000000000} kg/s; zero-flow-at-equal-pressure={backpressureRows[^1].MassFlowKilogramsPerSecond:0.000000000} kg/s;"),
            "backpressure-respected=True; mass-flow-nonincreasing=True; reverse-flow-blocked=True; internal-transfer-conservative=True",
            string.Empty);
        File.WriteAllText(Path.Combine(directory, $"{backpressureStem}.summary.txt"), backpressureSummary, new UTF8Encoding(false));
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

    private sealed record PressureSweepRow(
        double HeaderPressureMegapascals,
        double CondenserBackpressureMegapascals,
        double OpenFraction,
        double VaporAvailabilityFraction,
        double EffectiveThroatAreaSquareMillimetres,
        bool IsChoked,
        double MassFlowKilogramsPerSecond,
        double InternalEnergyTransferMegawatts);

    private sealed record BackpressureSweepRow(
        double PressureRatio,
        double CondenserBackpressureMegapascals,
        bool IsChoked,
        double MassFlowKilogramsPerSecond,
        double InternalEnergyTransferMegawatts);
}
