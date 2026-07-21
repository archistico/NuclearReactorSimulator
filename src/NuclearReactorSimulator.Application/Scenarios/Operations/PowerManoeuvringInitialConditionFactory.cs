using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Application.Scenarios.Operations;

/// <summary>
/// Exact M7.6 stable low-load parallel handoff. The built-in recipe seeds existing canonical owners only: reactor power,
/// rotor speed, committed breaker state and requested electrical load remain M2/M4/M5 state or inputs.
/// </summary>
public sealed class PowerManoeuvringInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation LowLoadSeed = NeutronPopulation.FromRelative(0.10d);
    private static readonly ControlRodPosition CriticalRodPosition = ControlRodPosition.FromPercentWithdrawn(50d);

    public InitialConditionDescriptor Descriptor { get; } = new(
        PowerManoeuvringNormalShutdownProgram.InitialCondition,
        "Stable Low-Load Parallel Operation v1",
        "Warm critical plant with main circulation established, turbine near synchronous speed, generator breaker closed and a 5 MWe requested low-load handoff for deterministic M7.6 manoeuvring and normal shutdown.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            LowLoadSeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 120d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d,
            initialGeneratorBreakerClosed: true,
            initialRequestedElectricalPowerMegawatts: 5d);
}
