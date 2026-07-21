using NuclearReactorSimulator.Domain.Physics.Fluids;

namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Resolves intensive thermodynamic variables from a node geometry and conserved inventory.
/// M1.2 defines this seam; M1.7 provides the first simplified production water/steam implementation.
/// </summary>
public interface IFluidThermodynamicModel
{
    FluidThermodynamicState Resolve(
        FluidNodeDefinition definition,
        FluidNodeInventory inventory,
        FluidThermodynamicState previousState);
}
