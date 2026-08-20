using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// I.5 REV1 exact-v4 activation-readiness identity. It preserves the exact-v2 physical seed and the exact-v3
/// corrected-commit hydraulic ownership, changing only the water/steam closure to the validated
/// CorrelationConsistentInverseDomain repair. Exact v2 and v3 remain immutable replay/rollback identities.
/// This factory is the authoritative production default after I.5 activation while exact v2/v3 remain immutable replay identities.
/// </summary>
public sealed class DesktopSustainedGenerationI5RepairedActivationCandidateInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 4);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v4 — I.5 Repaired Production",
        "I.5 REV1 repaired activation-readiness identity preserving the validated desktop physical seed, 10 ms fixed step and H.22 four-node corrected-commit ownership while selecting the validated CorrelationConsistentInverseDomain water/steam closure. Exact v2 explicit and exact v3 historical-closure corrected identities remain immutable and replayable; I.5 activates this exact v4 identity as the authoritative desktop production default.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => DesktopSustainedGenerationInitialConditionFactory
            .CreateRepairedCorrectedCommitProductionCandidateRuntimeEngine(RuntimeStep);
}
