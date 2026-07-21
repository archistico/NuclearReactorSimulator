using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed record CondenserCoolingBoundarySnapshot(
    string BoundaryId,
    string CondenserId,
    Power AvailableHeatRejectionPower,
    Power UsedHeatRejectionPower)
{
    public Power UnusedHeatRejectionPower => AvailableHeatRejectionPower - UsedHeatRejectionPower;
}
