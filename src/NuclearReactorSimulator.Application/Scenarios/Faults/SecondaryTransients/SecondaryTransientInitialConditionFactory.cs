using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;

/// <summary>
/// M8.4 transient-ready operating seed. It reuses the canonical M7 recipe while enabling a finite condenser cooling
/// boundary so loss/degradation changes the existing M4.3 heat-rejection constraint rather than inventing vacuum state.
/// </summary>
public sealed class SecondaryTransientInitialConditionFactory : IVersionedInitialConditionFactory
{
    public static InitialConditionReference InitialCondition { get; } = new("secondary-transient-ready", 1);

    public InitialConditionDescriptor Descriptor { get; } = new(
        InitialCondition,
        "Secondary-System Transient Ready v1",
        "Stable low-load parallel operation with finite condenser cooling capacity for deterministic M8.4 turbine, generator, feedwater and condenser transients.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            NeutronPopulation.FromRelative(0.10d),
            mainCirculationRunning: true,
            initialRodPosition: ControlRodPosition.FromPercentWithdrawn(50d),
            initialPrimaryTemperatureCelsius: 120d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 3_000d,
            initialGeneratorBreakerClosed: true,
            initialRequestedElectricalPowerMegawatts: 5d,
            // The reference condenser steam space is intentionally compact (10 m³). Keep the baseline
            // cooling sink finite but small enough that the 10 ms seed/runtime steps do not deplete a
            // disproportionate fraction of the conserved exhaust inventory before any transient begins.
            initialCondenserCoolingPowerMegawatts: 0.1d);
}
