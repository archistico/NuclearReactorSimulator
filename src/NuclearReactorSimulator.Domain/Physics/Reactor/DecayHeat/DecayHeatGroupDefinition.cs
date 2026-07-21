using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.DecayHeat;

/// <summary>
/// One equivalent first-order decay-heat inventory group.
/// </summary>
public sealed record DecayHeatGroupDefinition
{
    public DecayHeatGroupDefinition(
        string id,
        DecayHeatGenerationFraction generationFraction,
        DecayConstant decayConstant)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Decay-heat group id is required.", nameof(id))
            : id.Trim();

        if (generationFraction <= DecayHeatGenerationFraction.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(generationFraction),
                generationFraction,
                "Decay-heat group generation fraction must be greater than zero.");
        }

        if (decayConstant.PerSecond <= 0d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decayConstant),
                decayConstant,
                "Decay-heat group decay constant must be greater than zero.");
        }

        GenerationFraction = generationFraction;
        DecayConstant = decayConstant;
    }

    public string Id { get; }

    public DecayHeatGenerationFraction GenerationFraction { get; }

    public DecayConstant DecayConstant { get; }
}
