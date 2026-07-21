using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Net signed rates applied to a fluid node over one deterministic integration interval.
/// Positive values add inventory; negative values remove inventory.
/// </summary>
public readonly record struct FluidNodeBalance(
    MassFlowRate NetMassFlowRate,
    Power NetEnergyRate)
{
    public static FluidNodeBalance Zero { get; } = new(MassFlowRate.Zero, Power.Zero);

    public static FluidNodeBalance operator +(FluidNodeBalance left, FluidNodeBalance right)
    {
        return new FluidNodeBalance(
            left.NetMassFlowRate + right.NetMassFlowRate,
            left.NetEnergyRate + right.NetEnergyRate);
    }

    public static FluidNodeBalance operator -(FluidNodeBalance left, FluidNodeBalance right)
    {
        return new FluidNodeBalance(
            left.NetMassFlowRate - right.NetMassFlowRate,
            left.NetEnergyRate - right.NetEnergyRate);
    }
}
