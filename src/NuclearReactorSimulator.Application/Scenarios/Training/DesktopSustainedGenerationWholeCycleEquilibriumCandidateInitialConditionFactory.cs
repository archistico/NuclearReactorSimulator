using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M10 final LR-H1 exact-v6 analytical whole-cycle equilibrium candidate. Exact-v4 remains the authoritative
/// production identity and exact-v5 remains retained as failed diagnostic evidence. Version 6 changes only the
/// authored initial state and controller/pump biases required to close the already-valid production equations.
/// </summary>
public sealed class DesktopSustainedGenerationWholeCycleEquilibriumCandidateInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly TimeSpan RuntimeStep = TimeSpan.FromMilliseconds(10d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 6);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Sustained Generation Runtime v6 — Whole-Cycle Equilibrium Candidate",
        "M10 final LR-H1 analytical whole-cycle candidate. It preserves exact-v4 corrected-commit hydraulics, CorrelationConsistentInverseDomain closure, 10 ms runtime physics, component resistances and control laws while replacing the incomplete exact-v5 260 kg/s probe with an authored state that closes the 100 kg/s primary loop, 5 MWe turbine-generator mechanical demand, passive steam-path enthalpy, condenser UA balance, condensate/feedwater hydraulics and half-full drum inventory. Production selection remains exact-v4 until focused and cumulative qualification complete.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => DesktopSustainedGenerationInitialConditionFactory
            .CreateWholeCycleEquilibriumCandidateRuntimeEngine(RuntimeStep);
}
