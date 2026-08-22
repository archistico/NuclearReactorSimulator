using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M10 final LR-H1 exact-v5 reference operating-point candidate. It preserves exact-v4 replay semantics and
/// composes the already validated corrected-commit hydraulics plus CorrelationConsistentInverseDomain closure,
/// while replacing only the authored primary/thermal seed with a hydraulically and thermally coherent 260 kg/s
/// reference point. This candidate is not the authoritative production default until its focused and cumulative
/// validation ladder is complete.
/// </summary>
public sealed class DesktopSustainedGenerationReferenceOperatingPointCandidateInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 5);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v5 — Reference Operating-Point Candidate",
        "M10 final LR-H1 candidate preserving exact-v4 hydraulic/thermodynamic production semantics while introducing a distinct 260 kg/s reference operating-point seed with matched suction/pressure/outlet/drum pressure grade, saturated outlet thermal state and fuel/structure temperatures consistent with the unchanged 30 MW initial fission power. Exact v4 remains immutable and production selection is not switched by this candidate.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => DesktopSustainedGenerationInitialConditionFactory
            .CreateReferenceOperatingPointCandidateRuntimeEngine(RuntimeStep);
}
