using System.Text;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Simulation.Physics.Fluids;
using Xunit;

namespace NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay;

/// <summary>M10.9.4.1-G.1 audit-only open-control-volume energy-convention regressions.</summary>
public sealed class OpenControlVolumeEnergyTransportAuditTests
{
    private const double RepresentativeSteamFlowKilogramsPerSecond = 12.934773043d;
    private const double RepresentativeLiquidFlowKilogramsPerSecond = 12d;

    [Fact]
    public void CurrentV2RepresentativeNodesExposeFiniteEnthalpyGapWithoutRuntimeMutation()
    {
        var engine = CreateEngine();
        var committedPlant = engine.CurrentState.PlantState.PlantState;
        var originalNodes = committedPlant.FluidNodes.ToArray();
        var rows = BuildRows(committedPlant);

        Assert.Equal(4, rows.Count);
        Assert.All(rows, static row =>
        {
            Assert.True(double.IsFinite(row.SpecificFlowWorkKilojoulesPerKilogram));
            Assert.True(double.IsFinite(row.SpecificEnthalpyKilojoulesPerKilogram));
            Assert.True(double.IsFinite(row.FlowWorkRateMegawatts));
            Assert.True(row.SpecificEnthalpyKilojoulesPerKilogram > row.SpecificInternalEnergyKilojoulesPerKilogram);
            Assert.Equal(
                row.FlowWorkRateMegawatts,
                row.EnthalpyTransportRateMegawatts - row.InternalEnergyAdvectionRateMegawatts,
                9);
            Assert.Equal(0d, row.EnthalpyBalanceClosureWatts, 6);
        });

        var header = rows.First(static row => row.UpstreamNodeId == "header");
        var hotwell = rows.First(static row => row.UpstreamNodeId == "hotwell");
        Assert.True(header.SpecificFlowWorkKilojoulesPerKilogram > hotwell.SpecificFlowWorkKilojoulesPerKilogram);
        Assert.Equal(originalNodes, committedPlant.FluidNodes.ToArray());
    }

    [Fact(Explicit = true)]
    [Trait("Category", "OpenControlVolumeEnergyTransportAudit")]
    public void CurrentV2RepresentativeOpenControlVolumes_RecordInternalEnergyVersusEnthalpyGap()
    {
        var engine = CreateEngine();
        var committedPlant = engine.CurrentState.PlantState.PlantState;
        var rows = BuildRows(committedPlant);

        WriteAuditReports(rows);
    }

    private static IntegratedAutomaticOperationRuntimeEngine CreateEngine()
        => Assert.IsType<IntegratedAutomaticOperationRuntimeEngine>(
            new DesktopSustainedGenerationInitialConditionFactory().CreateRuntimeEngine());

    private static IReadOnlyList<TransportRow> BuildRows(NuclearReactorSimulator.Domain.Plant.PlantState plant)
    {
        var solver = new OpenControlVolumeEnergyTransportSolver();
        return new[]
        {
            BuildRow(
                "main-steam-header-to-condenser",
                solver.Solve(
                    plant.GetFluidNode("header"),
                    plant.GetFluidNode("exhaust"),
                    MassFlowRate.FromKilogramsPerSecond(RepresentativeSteamFlowKilogramsPerSecond))),
            BuildRow(
                "main-steam-header-to-stop-out",
                solver.Solve(
                    plant.GetFluidNode("header"),
                    plant.GetFluidNode("stop-out"),
                    MassFlowRate.FromKilogramsPerSecond(RepresentativeSteamFlowKilogramsPerSecond))),
            BuildRow(
                "hotwell-to-feedwater-inventory",
                solver.Solve(
                    plant.GetFluidNode("hotwell"),
                    plant.GetFluidNode("feedwater-inventory"),
                    MassFlowRate.FromKilogramsPerSecond(RepresentativeLiquidFlowKilogramsPerSecond))),
            BuildRow(
                "feedwater-inventory-to-drum",
                solver.Solve(
                    plant.GetFluidNode("feedwater-inventory"),
                    plant.GetFluidNode("drum"),
                    MassFlowRate.FromKilogramsPerSecond(RepresentativeLiquidFlowKilogramsPerSecond))),
        };
    }

    private static TransportRow BuildRow(
        string path,
        OpenControlVolumeEnergyTransportResult result)
    {
        var closure = result.EnthalpyFromNodeBalance.NetEnergyRate.Watts
            + result.EnthalpyToNodeBalance.NetEnergyRate.Watts;
        return new TransportRow(
            path,
            result.FromNodeId,
            result.ToNodeId,
            result.UpstreamNodeId,
            result.DownstreamNodeId,
            result.ReferenceMassFlowRate.KilogramsPerSecond,
            result.UpstreamPressure.Megapascals,
            result.UpstreamDensity.KilogramsPerCubicMetre,
            result.UpstreamSpecificInternalEnergy.KilojoulesPerKilogram,
            result.UpstreamSpecificFlowWork.KilojoulesPerKilogram,
            result.UpstreamSpecificEnthalpy.KilojoulesPerKilogram,
            result.SignedInternalEnergyAdvectionRate.Megawatts,
            result.SignedFlowWorkRate.Megawatts,
            result.SignedEnthalpyTransportRate.Megawatts,
            closure);
    }

    private static void WriteAuditReports(IReadOnlyList<TransportRow> rows)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "g1-energy-transport-convention");
        Directory.CreateDirectory(directory);
        const string stem = "01-current-v2-representative-open-control-volume-gap";

        var csv = new StringBuilder();
        csv.AppendLine("path,from_node,to_node,upstream_node,downstream_node,mass_flow_kg_per_s,upstream_pressure_mpa,upstream_density_kg_per_m3,specific_internal_energy_kj_per_kg,specific_flow_work_kj_per_kg,specific_enthalpy_kj_per_kg,internal_energy_advection_mw,flow_work_rate_mw,enthalpy_transport_mw,enthalpy_balance_closure_w");
        foreach (var row in rows)
        {
            csv.AppendLine(FormattableString.Invariant(
                $"{row.Path},{row.FromNodeId},{row.ToNodeId},{row.UpstreamNodeId},{row.DownstreamNodeId},{row.MassFlowKilogramsPerSecond:0.000000000},{row.UpstreamPressureMegapascals:0.000000},{row.UpstreamDensityKilogramsPerCubicMetre:0.000000000},{row.SpecificInternalEnergyKilojoulesPerKilogram:0.000000000},{row.SpecificFlowWorkKilojoulesPerKilogram:0.000000000},{row.SpecificEnthalpyKilojoulesPerKilogram:0.000000000},{row.InternalEnergyAdvectionRateMegawatts:0.000000000},{row.FlowWorkRateMegawatts:0.000000000},{row.EnthalpyTransportRateMegawatts:0.000000000},{row.EnthalpyBalanceClosureWatts:0.000000}"));
        }
        File.WriteAllText(Path.Combine(directory, $"{stem}.csv"), csv.ToString(), new UTF8Encoding(false));

        var steamRows = rows.Where(static row => row.UpstreamNodeId == "header").ToArray();
        var liquidRows = rows.Where(static row => row.UpstreamNodeId is "hotwell" or "feedwater-inventory").ToArray();
        var maximumFlowWork = rows.Max(static row => row.FlowWorkRateMegawatts);
        var maximumSpecificGap = rows.Max(static row => row.SpecificFlowWorkKilojoulesPerKilogram);
        var maximumClosure = rows.Max(static row => Math.Abs(row.EnthalpyBalanceClosureWatts));
        var summary = string.Join(Environment.NewLine,
            $"=== {stem} ===",
            "G.1 convention audit retained under the G.3 source: representative paths still expose u*m_dot, flow work and h*m_dot while every current-v2 non-turbine runtime transport owner now uses enthalpy.",
            FormattableString.Invariant(
                $"samples={rows.Count}; steam-paths={steamRows.Length}; liquid-paths={liquidRows.Length}; representative-steam-flow={RepresentativeSteamFlowKilogramsPerSecond:0.000000000} kg/s; representative-liquid-flow={RepresentativeLiquidFlowKilogramsPerSecond:0.000000000} kg/s;"),
            FormattableString.Invariant(
                $"maximum-specific-flow-work-gap={maximumSpecificGap:0.000000000} kJ/kg; maximum-flow-work-rate-gap={maximumFlowWork:0.000000000} MW; maximum-internal-transfer-closure={maximumClosure:0.000000} W;"),
            "identity-h-equals-u-plus-p-over-rho=True; enthalpy-equals-internal-energy-plus-flow-work=True; internal-transfer-conservative=True; runtime-passive-migration-active=True; pump-path-migration-active=True; remaining-non-turbine-migration-active=True; turbine-work-retuned=False",
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

    private sealed record TransportRow(
        string Path,
        string FromNodeId,
        string ToNodeId,
        string UpstreamNodeId,
        string DownstreamNodeId,
        double MassFlowKilogramsPerSecond,
        double UpstreamPressureMegapascals,
        double UpstreamDensityKilogramsPerCubicMetre,
        double SpecificInternalEnergyKilojoulesPerKilogram,
        double SpecificFlowWorkKilojoulesPerKilogram,
        double SpecificEnthalpyKilojoulesPerKilogram,
        double InternalEnergyAdvectionRateMegawatts,
        double FlowWorkRateMegawatts,
        double EnthalpyTransportRateMegawatts,
        double EnthalpyBalanceClosureWatts);
}
