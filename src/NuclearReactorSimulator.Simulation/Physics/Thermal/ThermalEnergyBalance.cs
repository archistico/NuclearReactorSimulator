using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Thermal;

/// <summary>
/// Net signed thermal power applied to one thermal domain.
/// Positive values add stored energy; negative values remove it.
/// </summary>
public readonly record struct ThermalEnergyBalance(Power NetHeatRate)
{
    public static ThermalEnergyBalance Zero { get; } = new(Power.Zero);

    public static ThermalEnergyBalance operator +(ThermalEnergyBalance left, ThermalEnergyBalance right)
    {
        return new ThermalEnergyBalance(left.NetHeatRate + right.NetHeatRate);
    }

    public static ThermalEnergyBalance operator -(ThermalEnergyBalance left, ThermalEnergyBalance right)
    {
        return new ThermalEnergyBalance(left.NetHeatRate - right.NetHeatRate);
    }
}
