using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.SteamDrums;

public sealed class SteamDrumSystemSnapshot
{
    public SteamDrumSystemSnapshot(
        SteamDrumSystemDefinition definition,
        IEnumerable<SteamDrumSnapshot> drums)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(drums);

        var canonical = drums.OrderBy(static item => item.DrumId, StringComparer.Ordinal).ToArray();
        var expectedIds = definition.Drums.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actualIds = canonical.Select(static item => item.DrumId).ToArray();
        if (!expectedIds.SequenceEqual(actualIds, StringComparer.Ordinal))
        {
            throw new ArgumentException("Steam-drum snapshot must contain exactly one snapshot per defined drum.", nameof(drums));
        }

        Definition = definition;
        Drums = new ReadOnlyCollection<SteamDrumSnapshot>(canonical);
        TotalIncomingReturnMassFlowRate = SumMassFlows(canonical.Select(static item => item.IncomingReturnMassFlowRate));
        TotalSeparatedSteamMassFlowRate = SumMassFlows(canonical.Select(static item => item.SeparatedSteamMassFlowRate));
        TotalRecirculatedLiquidMassFlowRate = SumMassFlows(canonical.Select(static item => item.RecirculatedLiquidMassFlowRate));
    }

    public SteamDrumSystemDefinition Definition { get; }

    public IReadOnlyList<SteamDrumSnapshot> Drums { get; }

    public MassFlowRate TotalIncomingReturnMassFlowRate { get; }

    public MassFlowRate TotalSeparatedSteamMassFlowRate { get; }

    public MassFlowRate TotalRecirculatedLiquidMassFlowRate { get; }

    public SteamDrumSnapshot GetDrum(string id)
        => Drums.FirstOrDefault(item => string.Equals(item.DrumId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown steam drum '{id}'.");

    private static MassFlowRate SumMassFlows(IEnumerable<MassFlowRate> values)
        => MassFlowRate.FromKilogramsPerSecond(CompensatedSum(values.Select(static value => value.KilogramsPerSecond)));

    private static double CompensatedSum(IEnumerable<double> values)
    {
        var sum = 0d;
        var compensation = 0d;
        foreach (var value in values)
        {
            var adjusted = value - compensation;
            var next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
        }

        return sum;
    }
}
