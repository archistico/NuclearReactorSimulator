using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M10 final LR-H1 exact-v7 governor-integral-reference candidate. It preserves the exact-v6 analytical
/// whole-cycle authored state while opting into the versioned breaker-closed droop integral-reference repair.
/// Exact-v4 remains production and exact-v5/exact-v6 remain retained diagnostic evidence.
/// </summary>
public sealed class DesktopSustainedGenerationGridDroopIntegralReferenceCandidateInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 7);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v7 — Grid-Droop Integral Reference Candidate",
        "M10 final LR-H1 candidate preserving the exact-v6 analytical whole-cycle authored state while changing only the versioned breaker-closed governor integral reference: proportional/derivative action retains the load-droop shifted speed reference, while integral action references synchronous grid speed so it cannot erase the intentional droop offset. Exact-v4 remains production until focused and cumulative qualification complete.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => DesktopSustainedGenerationInitialConditionFactory
            .CreateGridDroopIntegralReferenceCandidateRuntimeEngine(RuntimeStep);
}
