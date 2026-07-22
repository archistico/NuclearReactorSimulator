using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Supervisory;

/// <summary>M5-owned supervisory authority state. It owns no physical plant inventory or actuator position.</summary>
public sealed record SupervisoryOperationState
{
    public SupervisoryOperationState(
        PlantControlAuthorityMode requestedAuthority,
        PlantControlAuthorityMode effectiveAuthority,
        PlantControlAuthorityHealth health,
        string? degradationReason,
        SupervisoryOperatingObjective? objective,
        long transitionSequence)
    {
        if (!Enum.IsDefined(requestedAuthority))
        {
            throw new ArgumentOutOfRangeException(nameof(requestedAuthority));
        }
        if (!Enum.IsDefined(effectiveAuthority))
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveAuthority));
        }
        if (!Enum.IsDefined(health))
        {
            throw new ArgumentOutOfRangeException(nameof(health));
        }
        if (transitionSequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionSequence));
        }

        RequestedAuthority = requestedAuthority;
        EffectiveAuthority = effectiveAuthority;
        Health = health;
        DegradationReason = string.IsNullOrWhiteSpace(degradationReason) ? null : degradationReason.Trim();
        Objective = objective;
        TransitionSequence = transitionSequence;
    }

    public PlantControlAuthorityMode RequestedAuthority { get; }
    public PlantControlAuthorityMode EffectiveAuthority { get; }
    public PlantControlAuthorityHealth Health { get; }
    public string? DegradationReason { get; }
    public SupervisoryOperatingObjective? Objective { get; }
    public long TransitionSequence { get; }

    public static SupervisoryOperationState CreateInitial(PlantControlAuthorityMode initialAuthority)
        => new(initialAuthority, initialAuthority, PlantControlAuthorityHealth.Normal, null, null, 0);
}
