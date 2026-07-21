using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.Circulation;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;

public sealed class MainCirculationSystemSnapshot
{
    public MainCirculationSystemSnapshot(
        MainCirculationSystemDefinition definition,
        IEnumerable<MainCirculationLoopSnapshot> loops)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(loops);

        var canonicalLoops = loops.OrderBy(static item => item.LoopId, StringComparer.Ordinal).ToArray();
        if (canonicalLoops.Length != definition.Loops.Count)
        {
            throw new ArgumentException("Main-circulation snapshot must contain exactly one snapshot per defined loop.", nameof(loops));
        }

        Definition = definition;
        Loops = new ReadOnlyCollection<MainCirculationLoopSnapshot>(canonicalLoops);
        TotalPumpMassFlowRate = SumMassFlows(canonicalLoops.Select(static item => item.TotalPumpMassFlowRate));
        TotalChannelMassFlowRate = SumMassFlows(canonicalLoops.Select(static item => item.TotalChannelMassFlowRate));
        TotalReturnMassFlowRate = SumMassFlows(canonicalLoops.Select(static item => item.TotalReturnMassFlowRate));
        TotalHydraulicPowerExchange = SumPowers(canonicalLoops.Select(static item => item.TotalHydraulicPowerExchange));
        TotalShaftPowerDemand = SumPowers(canonicalLoops.Select(static item => item.TotalShaftPowerDemand));
    }

    public MainCirculationSystemDefinition Definition { get; }

    public IReadOnlyList<MainCirculationLoopSnapshot> Loops { get; }

    public MassFlowRate TotalPumpMassFlowRate { get; }

    public MassFlowRate TotalChannelMassFlowRate { get; }

    public MassFlowRate TotalReturnMassFlowRate { get; }

    public Power TotalHydraulicPowerExchange { get; }

    public Power TotalShaftPowerDemand { get; }

    public MainCirculationLoopSnapshot GetLoop(string id)
        => Loops.FirstOrDefault(item => string.Equals(item.LoopId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown main-circulation loop '{id}'.");

    private static MassFlowRate SumMassFlows(IEnumerable<MassFlowRate> values)
        => MassFlowRate.FromKilogramsPerSecond(CompensatedSum(values.Select(static value => value.KilogramsPerSecond)));

    private static Power SumPowers(IEnumerable<Power> values)
        => Power.FromWatts(CompensatedSum(values.Select(static value => value.Watts)));

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
