namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// Deployment-level desktop production selection. Exact v2 remains the explicit fail-closed rollback/reference,
/// exact v3 remains the historical H.29/H.30 corrected-commit production identity, and I.5 activates exact v4 with
/// the validated CorrelationConsistentInverseDomain thermodynamic repair plus the same corrected-commit ownership.
/// </summary>
public enum DesktopHydraulicProductionPolicy
{
    ExplicitCommittedState = 0,
    H29FourNodeCorrectedCommitCandidate = 1,
    I5RepairedFourNodeCorrectedCommit = 2,
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
/// Versioned desktop production selector. H.29/H.30 retain exact-v3 as a historical corrected identity; I.5 activates
/// exact-v4 as the authoritative repaired production identity after Stages 1-4 plus exact-v4 readiness qualification.
/// </summary>
public static class DesktopHydraulicProductionPolicySelector
{
    public static DesktopHydraulicProductionPolicy AuthoritativeDefaultPolicy
        => DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit;

    public static DesktopHydraulicProductionPolicy ExplicitRollbackPolicy
        => DesktopHydraulicProductionPolicy.ExplicitCommittedState;

    public static DesktopHydraulicProductionPolicy H29ActivationCandidatePolicy
        => DesktopHydraulicProductionPolicy.H29FourNodeCorrectedCommitCandidate;

    public static DesktopHydraulicProductionPolicy I5RepairedProductionPolicy
        => DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit;

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
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit
                => DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory.Reference,
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
            DesktopHydraulicProductionPolicy.I5RepairedFourNodeCorrectedCommit
                => new DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory(),
            _ => throw new ArgumentOutOfRangeException(nameof(decision), "Unknown effective desktop production policy."),
        };
    }
}
