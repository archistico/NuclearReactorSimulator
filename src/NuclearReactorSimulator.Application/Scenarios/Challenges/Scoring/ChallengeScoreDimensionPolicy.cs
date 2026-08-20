namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

public sealed record ChallengeScoreDimensionPolicy
{
    public ChallengeScoreDimensionPolicy(ChallengeScoreDimensionKind kind, decimal maximumPoints)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        if (maximumPoints <= 0m || maximumPoints > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPoints));
        }

        Kind = kind;
        MaximumPoints = maximumPoints;
    }

    public ChallengeScoreDimensionKind Kind { get; }
    public decimal MaximumPoints { get; }
}
