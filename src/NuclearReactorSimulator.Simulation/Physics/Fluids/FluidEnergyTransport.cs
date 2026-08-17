using NuclearReactorSimulator.Domain.Physics.Fluids;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Shared deterministic selector for the versioned advective-energy convention used by Phase G.
/// Fluid-node inventories remain internal-energy inventories; this helper only resolves the specific
/// energy and power carried across an open control-volume boundary.
/// </summary>
public static class FluidEnergyTransport
{
    public static SpecificEnergy ResolveSpecificFlowWork(Pressure pressure, Density density)
    {
        if (density <= Density.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(density), density, "Advective energy transport requires positive density.");
        }

        return SpecificEnergy.FromJoulesPerKilogram(
            pressure.Pascals / density.KilogramsPerCubicMetre);
    }

    public static SpecificEnergy ResolveSpecificEnthalpy(
        SpecificEnergy specificInternalEnergy,
        Pressure pressure,
        Density density)
        => SpecificEnergy.FromJoulesPerKilogram(
            specificInternalEnergy.JoulesPerKilogram
            + ResolveSpecificFlowWork(pressure, density).JoulesPerKilogram);

    public static SpecificEnergy ResolveSelectedSpecificEnergy(
        FluidEnergyTransportMode mode,
        SpecificEnergy specificInternalEnergy,
        Pressure pressure,
        Density density)
        => mode switch
        {
            FluidEnergyTransportMode.SpecificInternalEnergy => specificInternalEnergy,
            FluidEnergyTransportMode.SpecificEnthalpy => ResolveSpecificEnthalpy(
                specificInternalEnergy,
                pressure,
                density),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported fluid-energy transport mode."),
        };

    public static SpecificEnergy ResolveSelectedSpecificEnergy(
        FluidEnergyTransportMode mode,
        FluidNodeState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ResolveSelectedSpecificEnergy(
            mode,
            state.SpecificInternalEnergy,
            state.Pressure,
            state.Density);
    }

    public static Power ResolveSelectedEnergyRate(
        FluidEnergyTransportMode mode,
        FluidNodeState state,
        MassFlowRate massFlowRate)
        => ResolveSelectedSpecificEnergy(mode, state) * massFlowRate;
}
