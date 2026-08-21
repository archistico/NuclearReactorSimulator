using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Demand;

/// <summary>
/// Pure M10.9.6.2 projection from challenge/logical-step evidence to external-demand evidence. It has no dispatcher,
/// generator-setpoint, grid-coupling or supervisory-control owner.
/// </summary>
public static class ScenarioChallengeExternalDemandProjector
{
    public static ExternalEnergyDemandEvidenceSnapshot Project(
        ChallengeDefinition challenge,
        ChallengeLifecycleSnapshot lifecycle,
        ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(challenge);
        ArgumentNullException.ThrowIfNull(lifecycle);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!string.Equals(challenge.ExactId, lifecycle.ChallengeExactId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Challenge definition and lifecycle snapshot exact IDs must match.", nameof(lifecycle));
        }

        var alignedLifecycle = ChallengeLifecycleLogicalStepAlignment.Align(lifecycle, snapshot.LogicalStep);
        var profile = challenge.ExternalDemandProfile;
        if (profile is null || !alignedLifecycle.ActivatedLogicalStep.HasValue)
        {
            return ExternalEnergyDemandEvidenceSnapshot.Unavailable(snapshot.LogicalStep);
        }
        if (alignedLifecycle.ActivatedLogicalStep.Value > snapshot.LogicalStep)
        {
            throw new InvalidOperationException("Challenge activation logical step cannot be later than the evidence snapshot.");
        }

        var offset = snapshot.LogicalStep - alignedLifecycle.ActivatedLogicalStep.Value;
        var evaluated = profile.Evaluate(offset);
        var requested = ControlRoomElectricalEvidence.RequestedGeneratorLoadMegawatts(snapshot);
        var actual = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        double? error = actual.HasValue ? evaluated.DemandMegawatts - actual.Value : null;

        long? nextChangeLogicalStep = null;
        double? nextDemandMegawatts = null;
        if (profile.ExposeNextScheduledChange && evaluated.NextControlPoint is { } next)
        {
            nextChangeLogicalStep = alignedLifecycle.ActivatedLogicalStep.Value + next.OffsetLogicalStep;
            nextDemandMegawatts = next.DemandMegawatts;
        }

        return new ExternalEnergyDemandEvidenceSnapshot(
            true,
            profile.ExactId,
            snapshot.LogicalStep,
            offset,
            evaluated.DemandMegawatts,
            requested,
            actual,
            error,
            nextChangeLogicalStep,
            nextDemandMegawatts);
    }
}
