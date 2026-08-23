using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M10 final LR-H1 exact-v9 post-moisture analytical whole-cycle equilibrium candidate. It preserves the
/// validated exact-v8 governor and turbine moisture-drain semantics while recomputing the authored mass/energy
/// operating point around the phase-separated turbine admission model. Exact-v4 remains production.
/// </summary>
public sealed class DesktopSustainedGenerationPostMoistureEquilibriumCandidateInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 9);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v9 — Post-Moisture Equilibrium Candidate",
        "M10 final LR-H1 candidate preserving exact-v8 grid-droop integral-reference and explicit turbine moisture-drain ownership while recomputing the authored whole-cycle operating point for 13.028 kg/s work-producing vapor, 0.311 kg/s moisture drain and a conservative 5 MWe energy root. Exact-v4 remains production until focused and cumulative qualification complete.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => DesktopSustainedGenerationInitialConditionFactory
            .CreatePostMoistureEquilibriumCandidateRuntimeEngine(RuntimeStep);
}
