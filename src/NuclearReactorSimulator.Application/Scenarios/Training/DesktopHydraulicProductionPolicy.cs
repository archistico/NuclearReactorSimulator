namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// H.29 deployment-level hydraulic policy selection for the desktop current-v2 operating profile.
/// The authoritative default remains explicit until H.30 makes the final Phase H decision.
/// </summary>
public enum DesktopHydraulicProductionPolicy
{
    ExplicitCommittedState = 0,
    H29FourNodeCorrectedCommitCandidate = 1,
}

/// <summary>
/// Immutable result of one deployment policy resolution. The explicit kill request is fail-closed and always wins over
/// a corrected-candidate request. Exact initial-condition versions are used so save/replay identity is never reinterpreted.
/// </summary>
public sealed record DesktopHydraulicProductionPolicyDecision(
    DesktopHydraulicProductionPolicy RequestedPolicy,
    DesktopHydraulicProductionPolicy EffectivePolicy,
    InitialConditionReference InitialCondition,
    bool ExplicitKillApplied);

/// <summary>
/// H.29 production activation seam. It selects an immutable versioned factory before runtime construction; it never mutates
/// an already-running simulation and never changes the meaning of an existing initial-condition version.
/// </summary>
public static class DesktopHydraulicProductionPolicySelector
{
    public static DesktopHydraulicProductionPolicy AuthoritativeDefaultPolicy
        => DesktopHydraulicProductionPolicy.ExplicitCommittedState;

    public static DesktopHydraulicProductionPolicy H29ActivationCandidatePolicy
        => DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate;

    public static DesktopHydraulicProductionPolicyDecision Resolve(
        DesktopHydraulicProductionPolicy requestedPolicy,
        bool explicitKillRequested = false)
    {
        if (!Enum.IsDefined(requestedPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(requestedPolicy));
        }

        var effectivePolicy = explicitKillRequested
            ? DesktopHydraulicProductionPolicy.ExplicitCommittedState
            : requestedPolicy;
        var reference = effectivePolicy switch
        {
            DesktopHydraulicProductionPolicy.ExplicitCommittedState
                => DesktopSustainedGenerationInitialConditionFactory.Reference,
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate
                => DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory.Reference,
            _ => throw new ArgumentOutOfRangeException(nameof(requestedPolicy)),
        };

        return new DesktopHydraulicProductionPolicyDecision(
            requestedPolicy,
            effectivePolicy,
            reference,
            explicitKillRequested);
    }

    public static IVersionedInitialConditionFactory CreateFactory(DesktopHydraulicProductionPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return decision.EffectivePolicy switch
        {
            DesktopHydraulicProductionPolicy.ExplicitCommittedState
                => new DesktopSustainedGenerationInitialConditionFactory(),
            DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate
                => new DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory(),
            _ => throw new ArgumentOutOfRangeException(nameof(decision), "Unknown effective desktop hydraulic production policy."),
        };
    }
}
