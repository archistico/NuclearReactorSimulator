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
        if (snapshot.LogicalStep != lifecycle.LogicalStep)
        {
            throw new ArgumentException("External-demand projection requires the lifecycle and control-room snapshot from the same logical step.", nameof(snapshot));
        }

        var profile = challenge.ExternalDemandProfile;
        if (profile is null || !lifecycle.ActivatedLogicalStep.HasValue)
        {
            return ExternalEnergyDemandEvidenceSnapshot.Unavailable(snapshot.LogicalStep);
        }
        if (lifecycle.ActivatedLogicalStep.Value > snapshot.LogicalStep)
        {
            throw new InvalidOperationException("Challenge activation logical step cannot be later than the evidence snapshot.");
        }

        var offset = snapshot.LogicalStep - lifecycle.ActivatedLogicalStep.Value;
        var evaluated = profile.Evaluate(offset);
        var requested = RequestedGeneratorLoadMegawatts(snapshot);
        var actual = snapshot.Electrical.GrossElectricalOutput.NumericValue;
        double? error = actual.HasValue ? evaluated.DemandMegawatts - actual.Value : null;

        long? nextChangeLogicalStep = null;
        double? nextDemandMegawatts = null;
        if (profile.ExposeNextScheduledChange && evaluated.NextControlPoint is { } next)
        {
            nextChangeLogicalStep = lifecycle.ActivatedLogicalStep.Value + next.OffsetLogicalStep;
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

    private static double? RequestedGeneratorLoadMegawatts(ControlRoomSnapshot snapshot)
    {
        if (snapshot.Electrical.Generators.Count == 0)
        {
            return null;
        }

        var total = 0d;
        foreach (var generator in snapshot.Electrical.Generators)
        {
            if (!generator.RequestedElectricalPower.NumericValue.HasValue)
            {
                return null;
            }
            total += generator.RequestedElectricalPower.NumericValue.Value;
        }
        return total;
    }
}
