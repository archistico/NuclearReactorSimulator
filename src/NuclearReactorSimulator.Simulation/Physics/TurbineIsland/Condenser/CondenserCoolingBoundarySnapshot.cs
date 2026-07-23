using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.TurbineIsland.Condenser;

public sealed record CondenserCoolingBoundarySnapshot(
    string BoundaryId,
    string CondenserId,
    Temperature CoolantTemperature,
    Power AvailableHeatRejectionPower,
    Power SurfaceHeatTransferLimitedPower,
    Power EffectiveHeatRejectionCapacity,
    Power UsedHeatRejectionPower)
{
    public Power UnusedHeatRejectionPower => AvailableHeatRejectionPower - UsedHeatRejectionPower;
}
