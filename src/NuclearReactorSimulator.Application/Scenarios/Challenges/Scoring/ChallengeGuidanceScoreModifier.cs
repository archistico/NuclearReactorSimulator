using NuclearReactorSimulator.Application.Scenarios.Training;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

public sealed record ChallengeGuidanceScoreModifier
{
    public ChallengeGuidanceScoreModifier(TrainingGuidanceMode mode, decimal multiplier)
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

    public TrainingGuidanceMode Mode { get; }
    public decimal Multiplier { get; }
}
