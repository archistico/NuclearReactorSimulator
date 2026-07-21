using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.TurbineIsland.Feedwater;
using NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;
using NuclearReactorSimulator.Simulation.Plant;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Feedwater;

/// <summary>
/// Immutable M4.4 system snapshot over the inherited M3/M4 thermofluid integration result.
/// </summary>
public sealed class CondensateFeedwaterSystemSnapshot
{
    public CondensateFeedwaterSystemSnapshot(
        CondensateFeedwaterSystemDefinition definition,
        CondenserSystemSnapshot condenserSnapshot,
        IEnumerable<CondensateFeedwaterTrainSnapshot> trains)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CondenserSnapshot = condenserSnapshot ?? throw new ArgumentNullException(nameof(condenserSnapshot));
        ArgumentNullException.ThrowIfNull(trains);

        var canonical = trains.OrderBy(static item => item.TrainId, StringComparer.Ordinal).ToArray();
        Trains = new ReadOnlyCollection<CondensateFeedwaterTrainSnapshot>(canonical);
        TotalThermalConditioningPower = Power.FromWatts(canonical.Sum(static item => item.ThermalConditioningPower.Watts));
        TotalCondensatePumpShaftPowerDemand = Power.FromWatts(canonical.Sum(static item => item.CondensatePump.ShaftPowerDemand.Watts));
        TotalFeedwaterPumpShaftPowerDemand = Power.FromWatts(canonical.Sum(static item => item.FeedwaterPump.ShaftPowerDemand.Watts));
    }

    public CondensateFeedwaterSystemDefinition Definition { get; }

    public CondenserSystemSnapshot CondenserSnapshot { get; }

    public IReadOnlyList<CondensateFeedwaterTrainSnapshot> Trains { get; }

    public Power TotalThermalConditioningPower { get; }

    public Power TotalCondensatePumpShaftPowerDemand { get; }

    public Power TotalFeedwaterPumpShaftPowerDemand { get; }

    public PlantNetworkAudit ThermofluidAudit => CondenserSnapshot.ThermofluidAudit;

    public CondensateFeedwaterTrainSnapshot GetTrain(string id)
        => Trains.FirstOrDefault(item => string.Equals(item.TrainId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown condensate/feedwater train snapshot '{id}'.");
}
