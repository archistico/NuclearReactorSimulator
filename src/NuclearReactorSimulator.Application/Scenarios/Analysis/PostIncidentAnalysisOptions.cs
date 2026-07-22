namespace NuclearReactorSimulator.Application.Scenarios.Analysis;

/// <summary>
/// Deterministic M9.2 analysis-window request. No wall-clock duration is accepted: windows are expressed only in logical steps.
/// </summary>
public sealed record PostIncidentAnalysisOptions
{
    public PostIncidentAnalysisOptions(
        long preIncidentSteps = 20,
        long postIncidentSteps = 100,
        long? anchorEventSequence = null)
    {
        if (preIncidentSteps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(preIncidentSteps));
        }
        if (postIncidentSteps < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(postIncidentSteps));
        }
        if (anchorEventSequence.HasValue && anchorEventSequence.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(anchorEventSequence));
        }

        PreIncidentSteps = preIncidentSteps;
        PostIncidentSteps = postIncidentSteps;
        AnchorEventSequence = anchorEventSequence;
    }

    public long PreIncidentSteps { get; }
    public long PostIncidentSteps { get; }
    public long? AnchorEventSequence { get; }

    public static PostIncidentAnalysisOptions Default { get; } = new();
}
