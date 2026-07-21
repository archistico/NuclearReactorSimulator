using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Application.Scenarios.Synchronization;

/// <summary>
/// Exact M7.5 pre-synchronization handoff. It reuses the canonical M7.2 recipe while seeding the already-rolled rotor at
/// synchronous speed with matched initial electrical phase. Breaker closure remains a commanded M4.5 transition.
/// </summary>
public sealed class GridSynchronizationInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation LowPowerSeed = NeutronPopulation.FromRelative(0.05d);
    private static readonly ControlRodPosition CriticalRodPosition = ControlRodPosition.FromPercentWithdrawn(50d);

    public InitialConditionDescriptor Descriptor { get; } = new(
        GridSynchronizationLoadProgram.InitialCondition,
        "Pre-Synchronization / Initial Loading v1",
        "Warm low-power plant with main circulation established, turbine at synchronous speed, generator phase matched and breaker open for deliberate M7.5 synchronization and initial loading.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            LowPowerSeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 120d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d);
}
