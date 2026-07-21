using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.PrimaryCircuit.Circulation;

public sealed class MainCirculationLoopSnapshot
{
    public MainCirculationLoopSnapshot(
        string loopId,
        Pressure suctionHeaderPressure,
        Pressure pressureHeaderPressure,
        IEnumerable<MainCirculationPumpSnapshot> pumps,
        IEnumerable<MainCirculationBranchSnapshot> branches)
    {
        if (string.IsNullOrWhiteSpace(loopId))
        {
            throw new ArgumentException("Loop id cannot be empty or whitespace.", nameof(loopId));
        }

        ArgumentNullException.ThrowIfNull(pumps);
        ArgumentNullException.ThrowIfNull(branches);

        var canonicalPumps = pumps.OrderBy(static item => item.PumpId, StringComparer.Ordinal).ToArray();
        var canonicalBranches = branches.OrderBy(static item => item.FuelChannelGroupId, StringComparer.Ordinal).ToArray();
        if (canonicalPumps.Length == 0 || canonicalBranches.Length == 0)
        {
            throw new ArgumentException("A circulation-loop snapshot must contain at least one pump and one branch.");
        }

        LoopId = loopId.Trim();
        SuctionHeaderPressure = suctionHeaderPressure;
        PressureHeaderPressure = pressureHeaderPressure;
        HeaderPressureRise = pressureHeaderPressure - suctionHeaderPressure;
        Pumps = new ReadOnlyCollection<MainCirculationPumpSnapshot>(canonicalPumps);
        Branches = new ReadOnlyCollection<MainCirculationBranchSnapshot>(canonicalBranches);
        TotalPumpMassFlowRate = SumMassFlows(canonicalPumps.Select(static item => item.MassFlowRate));
        TotalChannelMassFlowRate = SumMassFlows(canonicalBranches.Select(static item => item.ChannelMassFlowRate));
        TotalReturnMassFlowRate = SumMassFlows(canonicalBranches.Select(static item => item.ReturnMassFlowRate));
        PumpToChannelContinuityResidual = TotalPumpMassFlowRate - TotalChannelMassFlowRate;
        ChannelToReturnContinuityResidual = TotalChannelMassFlowRate - TotalReturnMassFlowRate;
        TotalHydraulicPowerExchange = SumPowers(canonicalPumps.Select(static item => item.HydraulicPowerExchange));
        TotalShaftPowerDemand = SumPowers(canonicalPumps.Select(static item => item.ShaftPowerDemand));
    }

    public string LoopId { get; }

    public Pressure SuctionHeaderPressure { get; }

    public Pressure PressureHeaderPressure { get; }

    public PressureDifference HeaderPressureRise { get; }

    public IReadOnlyList<MainCirculationPumpSnapshot> Pumps { get; }

    public IReadOnlyList<MainCirculationBranchSnapshot> Branches { get; }

    public MassFlowRate TotalPumpMassFlowRate { get; }

    public MassFlowRate TotalChannelMassFlowRate { get; }

    public MassFlowRate TotalReturnMassFlowRate { get; }

    public MassFlowRate PumpToChannelContinuityResidual { get; }

    public MassFlowRate ChannelToReturnContinuityResidual { get; }

    public Power TotalHydraulicPowerExchange { get; }

    public Power TotalShaftPowerDemand { get; }

    public MainCirculationPumpSnapshot GetPump(string id)
        => Pumps.FirstOrDefault(item => string.Equals(item.PumpId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown main-circulation pump '{id}'.");

    public MainCirculationBranchSnapshot GetBranch(string fuelChannelGroupId)
        => Branches.FirstOrDefault(item => string.Equals(item.FuelChannelGroupId, fuelChannelGroupId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown main-circulation branch '{fuelChannelGroupId}'.");

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
