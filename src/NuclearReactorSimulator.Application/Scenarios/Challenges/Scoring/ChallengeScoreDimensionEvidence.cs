namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Scoring;

/// <summary>
/// Authored evaluator output for one scoring dimension. A normalized fraction is accepted only when evidence is available;
/// unavailable required evidence remains visible and scores zero rather than being assumed successful.
/// </summary>
public sealed record ChallengeScoreDimensionEvidence
{
    public ChallengeScoreDimensionEvidence(
        ChallengeScoreDimensionKind kind,
        bool isAvailable,
        decimal? performanceFraction,
        string evidenceSourceId,
        string evidenceSummary,
        bool isCriticalFailure = false)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceSourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(evidenceSummary);

        if (isAvailable)
        {
            if (!performanceFraction.HasValue || performanceFraction.Value < 0m || performanceFraction.Value > 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(performanceFraction), "Available score evidence requires a fraction from 0 through 1.");
            }
        }
        else
        {
            if (performanceFraction.HasValue)
            {
                throw new ArgumentException("Unavailable score evidence cannot carry a performance fraction.", nameof(performanceFraction));
            }
            if (isCriticalFailure)
            {
                throw new ArgumentException("Unavailable evidence cannot assert a critical failure.", nameof(isCriticalFailure));
            }
        }

        if (isCriticalFailure
            && kind is not ChallengeScoreDimensionKind.SafetyProtectionDiscipline
            and not ChallengeScoreDimensionKind.ProcedureRequiredActions)
        {
            throw new ArgumentException("Critical dominance failures are valid only for safety or procedure evidence.", nameof(isCriticalFailure));
        }

        Kind = kind;
        IsAvailable = isAvailable;
        PerformanceFraction = performanceFraction;
        EvidenceSourceId = evidenceSourceId.Trim();
        EvidenceSummary = evidenceSummary.Trim();
        IsCriticalFailure = isCriticalFailure;
    }

    public ChallengeScoreDimensionKind Kind { get; }
    public bool IsAvailable { get; }
    public decimal? PerformanceFraction { get; }
    public string EvidenceSourceId { get; }
    public string EvidenceSummary { get; }
    public bool IsCriticalFailure { get; }
}
