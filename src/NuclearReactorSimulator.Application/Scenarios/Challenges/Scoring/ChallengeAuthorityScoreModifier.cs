using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

public sealed record ChallengeAuthorityScoreModifier
{
    public ChallengeAuthorityScoreModifier(PlantControlAuthorityMode mode, decimal multiplier)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }
        if (multiplier <= 0m || multiplier > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        }

        Mode = mode;
        Multiplier = multiplier;
    }

    public PlantControlAuthorityMode Mode { get; }
    public decimal Multiplier { get; }
}
