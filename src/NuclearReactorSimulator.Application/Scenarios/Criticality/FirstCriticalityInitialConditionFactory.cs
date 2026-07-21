using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;

namespace NuclearReactorSimulator.Application.Scenarios.Criticality;

/// <summary>
/// M7.3 pre-criticality handoff recipe. It reuses the validated M7.2 construction path, starts main circulation at the
/// prepared handoff condition and supplies a tiny deterministic non-zero source-range neutron population. This is an
/// initial-condition seed for the existing homogeneous M2 point kinetics, not a second kinetics owner or hidden source model.
/// </summary>
public sealed class FirstCriticalityInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation SourceRangeSeed = NeutronPopulation.FromRelative(1e-8d);

    public InitialConditionDescriptor Descriptor { get; } = new(
        FirstCriticalityLowPowerProgram.InitialCondition,
        "Pre-Criticality / Source Range v1",
        "Prepared cold plant with main circulation established, steam path isolated, generator disconnected, rods inserted and a deterministic non-zero source-range kinetics seed.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            SourceRangeSeed,
            mainCirculationRunning: true);
}
