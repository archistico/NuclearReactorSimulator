using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Simplified saturation properties used by the educational M1.7 water/steam closure.
/// </summary>
public sealed record WaterSteamSaturationProperties(
    Temperature Temperature,
    Pressure Pressure,
    Density SaturatedLiquidDensity,
    Density SaturatedVaporDensity,
    SpecificEnergy SaturatedLiquidInternalEnergy,
    SpecificEnergy SaturatedVaporInternalEnergy)
{
    public double SaturatedLiquidSpecificVolumeCubicMetresPerKilogram => 1d / SaturatedLiquidDensity.KilogramsPerCubicMetre;

    public double SaturatedVaporSpecificVolumeCubicMetresPerKilogram => 1d / SaturatedVaporDensity.KilogramsPerCubicMetre;

    public SpecificEnergy LatentInternalEnergy => SpecificEnergy.FromJoulesPerKilogram(
        SaturatedVaporInternalEnergy.JoulesPerKilogram - SaturatedLiquidInternalEnergy.JoulesPerKilogram);
}
