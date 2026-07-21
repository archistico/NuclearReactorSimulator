using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Fluids;

/// <summary>
/// Defines the immutable geometric identity of a lumped fluid control volume.
/// </summary>
public sealed record FluidNodeDefinition
{
    public FluidNodeDefinition(string id, Volume volume)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A fluid node identifier cannot be empty or whitespace.", nameof(id));
        }

        if (volume <= Volume.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), volume, "A fluid node control volume must be greater than zero.");
        }

        Id = id.Trim();
        Volume = volume;
    }

    public string Id { get; }

    public Volume Volume { get; }
}
