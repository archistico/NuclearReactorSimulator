using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// H.29 separately reviewed production-default candidate for the validated desktop sustained-generation seed.
/// Version 3 changes only numerical-policy ownership from the v2 explicit reference to the already-qualified H.22
/// four-node corrected-commit path. Version 2 remains immutable and available for immediate deployment rollback.
/// </summary>
public sealed class DesktopSustainedGenerationH29ActivationCandidateInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 3);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v3 — H.29 Corrected-Commit Candidate",
        "H.29 production-default candidate preserving the validated v2 physical seed and 10 ms fixed step while selecting the H.22 four-node branch-continuity corrected-commit policy qualified through H.28 and the post-H.28 H.24 long-horizon requalification. The v2 explicit version remains the authoritative default and rollback reference until H.30 decides Phase H closure.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => DesktopSustainedGenerationInitialConditionFactory
            .CreateFourNodeCorrectedCommitProductionCandidateRuntimeEngine(RuntimeStep);
}
