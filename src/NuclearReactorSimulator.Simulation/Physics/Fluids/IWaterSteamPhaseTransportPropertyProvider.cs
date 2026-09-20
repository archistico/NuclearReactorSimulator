using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Internal water/steam capability for phase transport properties owned by the active thermodynamic closure.
/// This contract is intentionally separate from the public forward saturation-property provider because
/// transport bookkeeping and geometric/presentation saturation behavior have different ownership requirements.
/// </summary>
internal interface IWaterSteamPhaseTransportPropertyProvider
{
    WaterSteamPhaseTransportProperties GetSaturatedPhaseTransportProperties(
        Pressure pressure,
        Temperature temperature);
}

/// <summary>
/// Allocation-free saturated liquid/vapor properties used only for advected energy transport.
/// </summary>
internal readonly record struct WaterSteamPhaseTransportProperties(
    Density SaturatedLiquidDensity,
    Density SaturatedVaporDensity,
    SpecificEnergy SaturatedLiquidInternalEnergy,
    SpecificEnergy SaturatedVaporInternalEnergy);
