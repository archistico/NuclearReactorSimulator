using System.Text;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.MainSteam;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.MainSteam;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.TurbineIsland.MainSteam;

/// <summary>M10.9.4.1-F.1 deterministic compressible steam-capacity evidence.</summary>
public sealed class CompressibleSteamFlowCapacityAuditTests
{
    [Fact(Explicit = true)]
    [Trait("Category", "ChokedSteamFlowAudit")]
    public void CurrentV2RepresentativeSteamState_ProducesMonotonicSubcriticalToChokedCapacityMap()
    {
        var solver = new CompressibleSteamFlowSolver();
        var definition = new CompressibleSteamFlowDefinition(
            Area.FromSquareMillimetres(100d),
            dischargeCoefficient: 0.95d,
            specificGasConstant: SpecificGasConstant.FromJoulesPerKilogramKelvin(461.526d),
            heatCapacityRatio: 1.3d);
        var upstreamPressure = Pressure.FromMegapascals(6.2725d);
        var upstreamTemperature = Temperature.FromDegreesCelsius(278.5d);
        var rows = new List<CapacityAuditRow>();

        for (var index = 0; index <= 100; index++)
        {
            var pressureRatio = 1d - (index / 100d);
            var downstreamPressure = Pressure.FromPascals(upstreamPressure.Pascals * pressureRatio);
            var result = solver.Solve(
                definition,
                upstreamPressure,
                upstreamTemperature,
                downstreamPressure);
            rows.Add(new CapacityAuditRow(
                pressureRatio,
                downstreamPressure.Megapascals,
                result.IsChoked,
                result.MassFlowRate.KilogramsPerSecond));
        }

        Assert.Equal(101, rows.Count);
        Assert.Equal(0d, rows[0].MassFlowKilogramsPerSecond, 12);
        for (var index = 1; index < rows.Count; index++)
        {
            Assert.True(
                rows[index].MassFlowKilogramsPerSecond + 1e-12d
                >= rows[index - 1].MassFlowKilogramsPerSecond,
                Diagnostic(rows[index - 1], rows[index]));
        }

        var firstChoked = rows.First(static row => row.IsChoked);
        Assert.True(firstChoked.DownstreamToUpstreamPressureRatio <= definition.CriticalDownstreamToUpstreamPressureRatio);
        var chokedCapacity = rows[^1].MassFlowKilogramsPerSecond;
        foreach (var row in rows.Where(static row => row.IsChoked))
        {
            Assert.Equal(chokedCapacity, row.MassFlowKilogramsPerSecond, 12);
        }

        WriteArtifacts(
            rows,
            definition,
            upstreamPressure,
            upstreamTemperature,
            firstChoked,
            chokedCapacity);
    }

    private static void WriteArtifacts(
        IReadOnlyList<CapacityAuditRow> rows,
        CompressibleSteamFlowDefinition definition,
        Pressure upstreamPressure,
        Temperature upstreamTemperature,
        CapacityAuditRow firstChoked,
        double chokedCapacityKilogramsPerSecond)
    {
        var directory = EnsureAuditDirectory();
        var csv = new StringBuilder();
        csv.AppendLine("downstream_to_upstream_pressure_ratio,downstream_pressure_mpa,is_choked,mass_flow_kg_per_s");
        foreach (var row in rows)
        {
            csv.AppendLine(FormattableString.Invariant(
                $"{row.DownstreamToUpstreamPressureRatio:0.000000},{row.DownstreamPressureMegapascals:0.000000},{row.IsChoked},{row.MassFlowKilogramsPerSecond:0.000000000}"));
        }

        File.WriteAllText(
            Path.Combine(directory, "01-current-v2-representative-pressure-ratio-sweep.csv"),
            csv.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var criticalDownstreamPressureMegapascals = upstreamPressure.Megapascals
            * definition.CriticalDownstreamToUpstreamPressureRatio;
        var summary = new StringBuilder();
        summary.AppendLine("=== 01-current-v2-representative-pressure-ratio-sweep ===");
        summary.AppendLine("Ideal-vapor one-way nozzle/orifice capacity evidence only; no relief or bypass topology is active.");
        summary.AppendLine(FormattableString.Invariant(
            $"samples={rows.Count}; upstream-pressure={upstreamPressure.Megapascals:0.000000} MPa; upstream-temperature={upstreamTemperature.DegreesCelsius:0.000} C; full-open-area={definition.FullOpenThroatArea.SquareMillimetres:0.000} mm2; discharge-coefficient={definition.DischargeCoefficient:0.000};"));
        summary.AppendLine(FormattableString.Invariant(
            $"heat-capacity-ratio={definition.HeatCapacityRatio:0.000000}; specific-gas-constant={definition.SpecificGasConstant.JoulesPerKilogramKelvin:0.000} J/(kg K); analytic-critical-ratio={definition.CriticalDownstreamToUpstreamPressureRatio:0.000000}; analytic-critical-downstream-pressure={criticalDownstreamPressureMegapascals:0.000000} MPa;"));
        summary.AppendLine(FormattableString.Invariant(
            $"sampled-first-choked-ratio={firstChoked.DownstreamToUpstreamPressureRatio:0.000000}; choked-capacity={chokedCapacityKilogramsPerSecond:0.000000000} kg/s; projected-capacity-500mm2={chokedCapacityKilogramsPerSecond * 5d:0.000000000} kg/s; projected-capacity-1000mm2={chokedCapacityKilogramsPerSecond * 10d:0.000000000} kg/s;"));
        summary.AppendLine("mass-flow-monotonic=True; choked-plateau=True");
        summary.AppendLine();

        File.WriteAllText(
            Path.Combine(directory, "01-current-v2-representative-pressure-ratio-sweep.summary.txt"),
            summary.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EnsureAuditDirectory()
    {
        var directory = Path.Combine(FindRepositoryRoot(), "artifacts", "f1-choked-steam-flow");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NuclearReactorSimulator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the NuclearReactorSimulator repository root from the test output directory.");
    }

    private static string Diagnostic(CapacityAuditRow previous, CapacityAuditRow current)
        => string.Concat(
            "Compressible mass flow must not decrease when downstream pressure is reduced. ",
            FormattableString.Invariant(
                $"previous ratio={previous.DownstreamToUpstreamPressureRatio:0.000000}, flow={previous.MassFlowKilogramsPerSecond:0.000000000}; "),
            FormattableString.Invariant(
                $"current ratio={current.DownstreamToUpstreamPressureRatio:0.000000}, flow={current.MassFlowKilogramsPerSecond:0.000000000}."));

    private sealed record CapacityAuditRow(
        double DownstreamToUpstreamPressureRatio,
        double DownstreamPressureMegapascals,
        bool IsChoked,
        double MassFlowKilogramsPerSecond);
}
