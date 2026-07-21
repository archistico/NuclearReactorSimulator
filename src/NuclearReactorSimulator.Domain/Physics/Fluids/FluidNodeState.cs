using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Immutable state of one lumped fluid control volume.
/// </summary>
public sealed record FluidNodeState
{
    public FluidNodeState(
        FluidNodeDefinition definition,
        FluidNodeInventory inventory,
        FluidThermodynamicState thermodynamics)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(thermodynamics);

        Definition = definition;
        Inventory = inventory;
        Thermodynamics = thermodynamics;
    }

    public FluidNodeDefinition Definition { get; }

    public FluidNodeInventory Inventory { get; }

    public FluidThermodynamicState Thermodynamics { get; }

    public string Id => Definition.Id;

    public Volume Volume => Definition.Volume;

    public Mass Mass => Inventory.Mass;

    public Energy InternalEnergy => Inventory.InternalEnergy;

    public SpecificEnergy SpecificInternalEnergy => Inventory.SpecificInternalEnergy;

    public Pressure Pressure => Thermodynamics.Pressure;

    public Temperature Temperature => Thermodynamics.Temperature;

    public FluidPhase Phase => Thermodynamics.Phase;

    public VaporQuality? VaporQuality => Thermodynamics.VaporQuality;

    public Density Density => Mass / Volume;
}
