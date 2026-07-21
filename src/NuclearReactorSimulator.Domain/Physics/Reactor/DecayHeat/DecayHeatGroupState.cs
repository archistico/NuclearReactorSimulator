using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

/// <summary>
/// Conserved latent decay-energy inventory for one equivalent group.
/// </summary>
public sealed record DecayHeatGroupState
{
    public DecayHeatGroupState(string groupId, Energy storedDecayEnergy)
    {
        GroupId = string.IsNullOrWhiteSpace(groupId)
            ? throw new ArgumentException("Decay-heat group-state id is required.", nameof(groupId))
            : groupId.Trim();

        if (storedDecayEnergy < Energy.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(storedDecayEnergy),
                storedDecayEnergy,
                "Stored decay energy cannot be negative.");
        }

        StoredDecayEnergy = storedDecayEnergy;
    }

    public string GroupId { get; }

    public Energy StoredDecayEnergy { get; }
}
