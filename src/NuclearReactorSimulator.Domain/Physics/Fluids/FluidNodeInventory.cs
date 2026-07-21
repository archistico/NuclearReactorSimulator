using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Conserved extensive fluid inventory carried by a lumped control volume.
/// </summary>
public sealed record FluidNodeInventory
{
    public FluidNodeInventory(Mass mass, Energy internalEnergy)
    {
        if (mass <= Mass.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(mass), mass, "A fluid node must contain a strictly positive fluid mass.");
        }

        Mass = mass;
        InternalEnergy = internalEnergy;
    }

    public Mass Mass { get; }

    public Energy InternalEnergy { get; }

    public SpecificEnergy SpecificInternalEnergy => InternalEnergy / Mass;
}
