using System.Globalization;
using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Application.Scenarios.Synchronization;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Plant;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>M10.9.4.1-F.2 conservative pressure-actuated main-steam header relief regressions.</summary>
public sealed class MainSteamReliefImplementationTests
{
    [Fact]
    public void CurrentV2SustainedProfilesOwnSingleAtmosphericHeaderReliefWhileLegacyRemainsUnchanged()
    {
        var desktop = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var synchronization = CreateEngine(new GridSynchronizationSustainedInitialConditionFactory());
        var legacy = CreateEngine(new ColdShutdownInitialConditionFactory());

        AssertReliefContract(desktop);
        AssertReliefContract(synchronization);
        Assert.Empty(legacy.CurrentState.PlantDefinition.TurbineExpansionSystem.MainSteamNetwork.ReliefBoundaries);
    }

    [Fact]
    public void CurrentV2OperationalSeedsRemainBelowReliefSetPressureAndPublishClosedSnapshot()
    {
        foreach (var engine in new[]
                 {
                     CreateEngine(new DesktopSustainedGenerationInitialConditionFactory()),
                     CreateEngine(new GridSynchronizationSustainedInitialConditionFactory()),
                 })
        {
            var relief = Assert.Single(engine.LatestCanonicalSnapshot.Control.ProtectedControl
                .FullPlant.IntegratedCycle.TurbineExpansion.MainSteamNetwork.ReliefBoundaries);

            Assert.True(relief.SourcePressure < Pressure.FromMegapascals(6.5d));
            Assert.Equal(0d, relief.LiftFraction, 12);
            Assert.Equal(MassFlowRate.Zero, relief.MassFlowRate);
            Assert.Equal(Power.Zero, relief.EnergyExportRate);
        }
    }

    [Fact(Explicit = true)]
    [Trait("Category", "MainSteamReliefImplementationAudit")]
    public void CurrentV2HeaderPressureSweep_RecordsLiftCapacityAndConservativeBoundaryExchange()
    {
        var engine = CreateEngine(new DesktopSustainedGenerationInitialConditionFactory());
        var mainSteam = engine.CurrentState.PlantDefinition.TurbineExpansionSystem.MainSteamNetwork;
        var solver = new MainSteamReliefBoundarySolver(mainSteam);
        var committedPlant = engine.CurrentState.PlantState.PlantState;
        var rows = new List<ReliefAuditRow>();

        for (var index = 0; index <= 60; index++)
        {
            var pressureMegapascals = 6.2d + (index * 0.01d);
            var state = WithHeaderPressure(committedPlant, pressureMegapascals);
            var result = solver.Solve(state);
            var snapshot = Assert.Single(result.Snapshots);
            var balance = Assert.Single(result.SourceTerms.FluidNodeBalances);

            Assert.Equal("header", balance.Key);
            Assert.Equal(-snapshot.MassFlowRate.KilogramsPerSecond, balance.Value.NetMassFlowRate.KilogramsPerSecond, 10);
            Assert.Equal(-snapshot.EnergyExportRate.Watts, balance.Value.NetEnergyRate.Watts, 3);
            Assert.Equal(-snapshot.MassFlowRate.KilogramsPerSecond, result.SourceTerms.ExternalMassFlowRate.KilogramsPerSecond, 10);
            Assert.Equal(-snapshot.EnergyExportRate.Watts, result.SourceTerms.ExternalPower.Watts, 3);

            rows.Add(new ReliefAuditRow(
                pressureMegapascals,
                snapshot.LiftFraction,
                snapshot.VaporAvailabilityFraction,
                snapshot.EffectiveThroatArea.SquareMillimetres,
                snapshot.IsChoked,
                snapshot.MassFlowRate.KilogramsPerSecond,
                snapshot.EnergyExportRate.Megawatts));
        }

        Assert.All(rows.Where(static row => row.HeaderPressureMegapascals <= 6.5d),
            static row => Assert.Equal(0d, row.MassFlowKilogramsPerSecond, 12));
        Assert.All(rows.Where(static row => row.HeaderPressureMegapascals >= 6.7d),
            static row => Assert.Equal(1d, row.LiftFraction, 12));
        Assert.True(rows.Zip(rows.Skip(1), static (left, right) => right.MassFlowKilogramsPerSecond >= left.MassFlowKilogramsPerSecond - 1e-10d).All(static value => value));
        Assert.True(rows[^1].MassFlowKilogramsPerSecond > 12d);
        Assert.True(rows.Where(static row => row.MassFlowKilogramsPerSecond > 0d).All(static row => row.IsChoked));

        WriteAuditReport(rows);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine(IVersionedInitialConditionFactory factory)
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(factory.CreateRuntimeEngine());

    private static void AssertReliefContract(IntegratedAutomaticOperationRuntimeEngine engine)
    {
        var relief = Assert.Single(engine.CurrentState.PlantDefinition.TurbineExpansionSystem.MainSteamNetwork.ReliefBoundaries);
        Assert.Equal("header-relief", relief.Id);
        Assert.Equal("header", relief.SourceHeaderNodeId);
        Assert.Equal("atmospheric-relief-receiver", relief.ReceiverBoundaryId);
        Assert.Equal(Pressure.StandardAtmosphere, relief.ReceiverPressure);
        Assert.Equal(6.5d, relief.SetPressure.Megapascals, 12);
        Assert.Equal(6.7d, relief.FullLiftPressure.Megapascals, 12);
        Assert.Equal(1_600d, relief.FlowDefinition.FullOpenThroatArea.SquareMillimetres, 12);
    }

    private static PlantState WithHeaderPressure(PlantState source, double pressureMegapascals)
    {
        var fluidNodes = source.FluidNodes.Select(node =>
        {
            if (!string.Equals(node.Id, "header", StringComparison.Ordinal))
            {
                return node;
            }

            return new FluidNodeState(
                node.Definition,
                node.Inventory,
                new FluidThermodynamicState(
                    Pressure.FromMegapascals(pressureMegapascals),
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

    private static void WriteAuditReport(IReadOnlyList<ReliefAuditRow> rows)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "f2-main-steam-relief");
        Directory.CreateDirectory(directory);
        var stem = "01-current-v2-header-relief-pressure-sweep";
        var csv = new StringBuilder();
        csv.AppendLine("header_pressure_mpa,lift_fraction,vapor_availability_fraction,effective_throat_area_mm2,is_choked,mass_flow_kg_per_s,energy_export_mw");
        foreach (var row in rows)
        {
            csv.AppendLine(FormattableString.Invariant(
                $"{row.HeaderPressureMegapascals:0.000000},{row.LiftFraction:0.000000},{row.VaporAvailabilityFraction:0.000000},{row.EffectiveThroatAreaSquareMillimetres:0.000000},{row.IsChoked},{row.MassFlowKilogramsPerSecond:0.000000000},{row.EnergyExportMegawatts:0.000000000}"));
        }

        File.WriteAllText(Path.Combine(directory, $"{stem}.csv"), csv.ToString(), new UTF8Encoding(false));
        var firstOpen = rows.First(static row => row.MassFlowKilogramsPerSecond > 0d);
        var firstFullLift = rows.First(static row => row.LiftFraction >= 1d);
        var maximum = rows[^1];
        var summary = string.Join(Environment.NewLine,
            $"=== {stem} ===",
            "Pressure-actuated atmospheric header relief over the validated F.1 ideal-vapor capacity seam; no turbine bypass or receiver inventory is active.",
            FormattableString.Invariant(
                $"samples={rows.Count}; pressure={rows[0].HeaderPressureMegapascals:0.000000}..{maximum.HeaderPressureMegapascals:0.000000} MPa; set-pressure=6.500000 MPa; full-lift-pressure=6.700000 MPa; receiver-pressure={Pressure.StandardAtmosphere.Megapascals:0.000000} MPa;"),
            FormattableString.Invariant(
                $"first-open-pressure={firstOpen.HeaderPressureMegapascals:0.000000} MPa; first-full-lift-pressure={firstFullLift.HeaderPressureMegapascals:0.000000} MPa; vapor-availability={maximum.VaporAvailabilityFraction:0.000000}; full-lift-capacity-at-{maximum.HeaderPressureMegapascals:0.00}MPa={maximum.MassFlowKilogramsPerSecond:0.000000000} kg/s; energy-export={maximum.EnergyExportMegawatts:0.000000000} MW;"),
            "mass-flow-monotonic=True; relief-exchange-conservative=True; turbine-bypass-active=False",
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

    private sealed record ReliefAuditRow(
        double HeaderPressureMegapascals,
        double LiftFraction,
        double VaporAvailabilityFraction,
        double EffectiveThroatAreaSquareMillimetres,
        bool IsChoked,
        double MassFlowKilogramsPerSecond,
        double EnergyExportMegawatts);
}
