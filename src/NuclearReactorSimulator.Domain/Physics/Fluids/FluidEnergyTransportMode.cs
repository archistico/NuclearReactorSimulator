namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Selects the specific energy carried by one hydraulic connection.
/// Historical definitions advect specific internal energy. Phase G current-v2 passive paths
/// may opt in to open-control-volume specific enthalpy while node inventories continue to store internal energy.
/// </summary>
public enum FluidEnergyTransportMode
{
    SpecificInternalEnergy = 0,
    SpecificEnthalpy = 1,
}
