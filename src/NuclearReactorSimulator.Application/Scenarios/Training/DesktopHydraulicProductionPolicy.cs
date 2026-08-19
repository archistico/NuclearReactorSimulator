namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// Deployment-level hydraulic policy selection for the desktop current-v2/current-v3 operating profile.
/// H.30 Requalification 1 promotes the already-qualified exact-v3 corrected-commit path to the authoritative default,
/// while exact v2 remains the explicit rollback/reference policy.
/// </summary>
public enum DesktopHydraulicProductionPolicy
{
    ExplicitCommittedState = 0,
    H29FourNodeCorrectedCommitCandidate = 1,
}

/// <summary>
/// Immutable result of one deployment policy resolution. The explicit kill request is fail-closed and always wins over
/// the authoritative/default corrected policy. Exact initial-condition versions are used so save/replay identity is never
/// reinterpreted.
/// </summary>
public sealed record DesktopHydraulicProductionPolicyDecision(
    DesktopHydraulicProductionPolicy RequestedPolicy,
    DesktopHydraulicProductionPolicy EffectivePolicy,
    InitialConditionReference InitialCondition,
    bool ExplicitKillApplied);

/// <summary>
/// Versioned desktop hydraulic production selector. H.29 introduced the exact-v3 candidate and fail-closed v2 kill seam;
/// H.30 Requalification 1 changes only which already-qualified exact version is authoritative by default.
/// </summary>
public static class DesktopHydraulicProductionPolicySelector
{
    public static DesktopHydraulicProductionPolicy AuthoritativeDefaultPolicy
        => DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate;

    public static DesktopHydraulicProductionPolicy ExplicitRollbackPolicy
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
