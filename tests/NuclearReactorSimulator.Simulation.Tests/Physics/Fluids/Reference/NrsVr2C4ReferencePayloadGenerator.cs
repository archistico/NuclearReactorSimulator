using System.Security.Cryptography;
using System.Text;

namespace NuclearReactorSimulator.Simulation.Tests.Physics.Fluids.Reference;

/// <summary>
/// Offline C# generator for the selected C4 production payload. It is test/reference-only and never
/// referenced by production. Generation order and encoding are frozen by R1 Implementation Planning 1.
/// </summary>
internal static class NrsVr2C4ReferencePayloadGenerator
{
    private const double DenseSaturationStepKelvins = 0.02d;
    private const double PrefixSaturationStepKelvins = 0.5d;

    private static readonly double[] LiquidPressureNodesMegapascals =
    [
        0.02d, 0.05d, 0.1d, 0.2d, 0.5d, 1d, 2d, 5d, 7d, 10d, 15d, 20d, 30d, 50d, 75d, 100d,
    ];

    private static readonly double[] LiquidBoundaryOffsetsMegapascals =
    [
        0.001d, 0.005d, 0.02d, 0.1d, 0.5d, 2d, 5d,
    ];

    private static readonly double[] VaporPressureNodesMegapascals =
    [
        0.001d, 0.002d, 0.005d, 0.01d, 0.02d, 0.05d, 0.1d, 0.2d, 0.5d, 1d, 2d, 5d, 10d, 15d, 20d,
    ];

    private static readonly double[] VaporBoundaryFractions =
    [
        0.999999d, 0.9999d, 0.999d, 0.99d, 0.95d, 0.9d, 0.75d, 0.5d, 0.25d, 0.1d,
    ];

    internal static byte[] GeneratePayloadBytes()
    {
        var dense = BuildSaturation(DenseSaturationStepKelvins);
        var prefix = BuildSaturation(PrefixSaturationStepKelvins);
        var liquid = BuildLiquidRows();
        var vapor = BuildVaporRows();

        using var memory = new MemoryStream();
        using (var writer = new BinaryWriter(memory, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("NRSVR2C4"));
            writer.Write(1);
            writer.Write(0x01020304);
            writer.Write(dense.Length);
            writer.Write(prefix.Length);
            writer.Write(liquid.Length);
            writer.Write(vapor.Length);

            WriteNodes(writer, dense);
            WriteNodes(writer, prefix);
            WriteRows(writer, liquid);
            WriteRows(writer, vapor);
        }

        return memory.ToArray();
    }

    internal static string Sha256Hex(ReadOnlySpan<byte> bytes)
        => Convert.ToHexString(SHA256.HashData(bytes));

    private static SaturationNode[] BuildSaturation(double stepKelvins)
    {
        var nodes = new List<SaturationNode>();
        for (var temperature = 273.15d; temperature < 623.15d - 1e-12d; temperature += stepKelvins)
        {
            nodes.Add(CreateNode(temperature));
        }
        nodes.Add(CreateNode(623.15d));
        return nodes.ToArray();
    }

    private static SaturationNode CreateNode(double temperatureKelvins)
    {
        var pressure = IapwsIf97Reference.SaturationPressureMegapascals(temperatureKelvins);
        var liquid = IapwsIf97Reference.Region1(temperatureKelvins, pressure);
        var vapor = IapwsIf97Reference.Region2(temperatureKelvins, pressure);

        var liquidProbePressure = Math.Min(100d, pressure + 0.25d);
        var compressed = IapwsIf97Reference.Region1(temperatureKelvins, liquidProbePressure);
        var saturatedDensity = liquid.DensityKilogramsPerCubicMetre;
        var compressedDensity = compressed.DensityKilogramsPerCubicMetre;
        var compression = (compressedDensity / saturatedDensity) - 1d;
        var bulkModulus = compression > 0d
            ? ((liquidProbePressure - pressure) * 1_000_000d) / compression
            : double.NaN;

        var vaporProbePressure = Math.Max(1e-6d, pressure * 0.99d);
        var expandedVapor = IapwsIf97Reference.Region2(temperatureKelvins, vaporProbePressure);
        var volumeDelta = expandedVapor.SpecificVolumeCubicMetresPerKilogram - vapor.SpecificVolumeCubicMetresPerKilogram;
        var vaporSlope = Math.Abs(volumeDelta) > 1e-30d
            ? (vaporProbePressure - pressure) / volumeDelta
            : double.NaN;

        return new SaturationNode(
            temperatureKelvins,
            pressure,
            liquid.SpecificVolumeCubicMetresPerKilogram,
            liquid.SpecificInternalEnergyJoulesPerKilogram,
            vapor.SpecificVolumeCubicMetresPerKilogram,
            vapor.SpecificInternalEnergyJoulesPerKilogram,
            bulkModulus,
            vaporSlope);
    }

    private static TemperatureRow[] BuildLiquidRows()
    {
        var rows = new List<TemperatureRow>();
        foreach (var temperature in TemperatureGrid(273.15d, 623.15d, 1d))
        {
            var saturationPressure = IapwsIf97Reference.SaturationPressureMegapascals(temperature);
            var states = new List<IapwsReferenceState>();
            AddUnique(states, IapwsIf97Reference.Region1(temperature, saturationPressure));

            foreach (var offset in LiquidBoundaryOffsetsMegapascals)
            {
                var pressure = saturationPressure + offset;
                if (pressure <= 100d)
                {
                    AddUnique(states, IapwsIf97Reference.Region1(temperature, pressure));
                }
            }

            foreach (var pressure in LiquidPressureNodesMegapascals)
            {
                if (pressure <= saturationPressure * (1d + 1e-12d))
                {
                    continue;
                }
                AddUnique(states, IapwsIf97Reference.Region1(temperature, pressure));
            }

            if (states.Count >= 2)
            {
                rows.Add(new TemperatureRow(
                    temperature,
                    states
                        .Select(static state => new TablePoint(
                            state.PressureMegapascals,
                            state.SpecificVolumeCubicMetresPerKilogram,
                            state.SpecificInternalEnergyJoulesPerKilogram))
                        .OrderByDescending(static point => point.SpecificVolume)
                        .ToArray()));
            }
        }

        return rows.ToArray();
    }

    private static TemperatureRow[] BuildVaporRows()
    {
        var rows = new List<TemperatureRow>();
        foreach (var temperature in TemperatureGrid(273.15d, 1_073.15d, 2d))
        {
            var maximumPressure = Rp1bIf97Domain.Region2MaximumPressureMegapascals(temperature);
            var states = new List<IapwsReferenceState>();
            AddUnique(states, IapwsIf97Reference.Region2(temperature, maximumPressure));

            foreach (var fraction in VaporBoundaryFractions)
            {
                var pressure = maximumPressure * fraction;
                if (pressure > 1e-6d)
                {
                    AddUnique(states, IapwsIf97Reference.Region2(temperature, pressure));
                }
            }

            foreach (var pressure in VaporPressureNodesMegapascals)
            {
                if (pressure > maximumPressure * (1d + 1e-12d))
                {
                    continue;
                }
                AddUnique(states, IapwsIf97Reference.Region2(temperature, pressure));
            }

            if (states.Count >= 2)
            {
                rows.Add(new TemperatureRow(
                    temperature,
                    states
                        .Select(static state => new TablePoint(
                            state.PressureMegapascals,
                            state.SpecificVolumeCubicMetresPerKilogram,
                            state.SpecificInternalEnergyJoulesPerKilogram))
                        .OrderByDescending(static point => point.SpecificVolume)
                        .ToArray()));
            }
        }

        return rows.ToArray();
    }

    private static void AddUnique(List<IapwsReferenceState> states, IapwsReferenceState candidate)
    {
        if (states.Any(state => Math.Abs(state.PressureMegapascals - candidate.PressureMegapascals) <= 1e-12d))
        {
            return;
        }
        states.Add(candidate);
    }

    private static IEnumerable<double> TemperatureGrid(double minimum, double maximum, double step)
    {
        for (var value = minimum; value < maximum - 1e-12d; value += step)
        {
            yield return value;
        }
        yield return maximum;
    }

    private static void WriteNodes(BinaryWriter writer, SaturationNode[] nodes)
    {
        foreach (var node in nodes)
        {
            writer.Write(node.TemperatureKelvins);
            writer.Write(node.PressureMegapascals);
            writer.Write(node.LiquidSpecificVolume);
            writer.Write(node.LiquidSpecificEnergy);
            writer.Write(node.VaporSpecificVolume);
            writer.Write(node.VaporSpecificEnergy);
            writer.Write(node.NearBoundaryLiquidBulkModulusPascals);
            writer.Write(node.NearBoundaryVaporPressurePerSpecificVolumeSlope);
        }
    }

    private static void WriteRows(BinaryWriter writer, TemperatureRow[] rows)
    {
        foreach (var row in rows)
        {
            writer.Write(row.TemperatureKelvins);
            writer.Write(row.Points.Length);
            foreach (var point in row.Points)
            {
                writer.Write(point.PressureMegapascals);
                writer.Write(point.SpecificVolume);
                writer.Write(point.SpecificEnergy);
            }
        }
    }

    private readonly record struct SaturationNode(
        double TemperatureKelvins,
        double PressureMegapascals,
        double LiquidSpecificVolume,
        double LiquidSpecificEnergy,
        double VaporSpecificVolume,
        double VaporSpecificEnergy,
        double NearBoundaryLiquidBulkModulusPascals,
        double NearBoundaryVaporPressurePerSpecificVolumeSlope);

    private readonly record struct TablePoint(
        double PressureMegapascals,
        double SpecificVolume,
        double SpecificEnergy);

    private sealed record TemperatureRow(
        double TemperatureKelvins,
        TablePoint[] Points);
}
