using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Optional water/steam-specific thermodynamic capability for components that require explicit saturation properties.
/// Generic fluid solvers continue to depend only on <see cref="IFluidThermodynamicModel"/>.
/// </summary>
public interface IWaterSteamSaturationPropertyProvider
{
    WaterSteamSaturationProperties GetSaturationProperties(Temperature temperature);

    WaterSteamSaturationProperties GetSaturationProperties(Pressure pressure);
}
