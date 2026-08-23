using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M10 final LR-H1 exact-v8 turbine-admission moisture-owner candidate. It preserves the exact-v7 authored
/// whole-cycle state and grid-droop integral-reference repair while assigning the non-vapor admission fraction
/// to an explicit hotwell moisture drain instead of leaving it indefinitely in turbine-inlet.
/// Exact-v4 remains production until focused and cumulative qualification complete.
/// </summary>
public sealed class DesktopSustainedGenerationMoistureDrainCandidateInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 8);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v8 — Turbine Moisture-Drain Candidate",
        "M10 final LR-H1 candidate preserving exact-v7 authored state and breaker-closed governor integral-reference repair while versioning turbine admission so vapor remains the sole work-producing stage flow and rejected non-vapor mass/energy receives an explicit hotwell moisture-drain owner. Exact-v4 remains production until focused and cumulative qualification complete.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => DesktopSustainedGenerationInitialConditionFactory
            .CreateMoistureDrainCandidateRuntimeEngine(RuntimeStep);
}
