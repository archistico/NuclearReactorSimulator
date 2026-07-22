using NuclearReactorSimulator.Application.Scenarios;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Application.Scenarios.Training;

/// <summary>
/// M9.7 desktop-stability initial condition. It deliberately uses a new versioned identity instead of mutating the validated
/// M7.6/M7.7 low-load seed. The turbine steam path starts with a physically resolvable warm inventory, the validated
/// 5 MWe low-load request, a small finite condenser-cooling boundary and governor droop/opening. Saturation-boundary
/// robustness belongs to the canonical thermodynamic resolver rather than to an inflated desktop-only liquid-density margin.
/// Historical M7 seeds keep their original defaults and exact identities unchanged.
/// </summary>
public sealed class DesktopIntegratedOperationsInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation LowLoadSeed = NeutronPopulation.FromRelative(0.10d);
    private static readonly ControlRodPosition CriticalRodPosition = ControlRodPosition.FromPercentWithdrawn(50d);

    public static InitialConditionReference Reference { get; } = new("integrated-operations-desktop-stable", 1);

    public InitialConditionDescriptor Descriptor { get; } = new(
        Reference,
        "Integrated Operations Desktop Stable Runtime v1",
        "M9.7 desktop integration seed preserving the M7.7 low-load training intent while using an explicitly versioned balanced low-load steam/turbine/condenser lineup for sustained RUN-mode validation.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            LowLoadSeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 120d,
            turbineStartupLineup: true,
            initialRotorSpeedRpm: 2_995d,
            initialGeneratorBreakerClosed: true,
            initialRequestedElectricalPowerMegawatts: 5d,
            initialCondenserCoolingPowerMegawatts: 0.1d,
            initialTurbineSpeedSetpointRpm: 3_000d,
            initialSteamPathTemperatureCelsius: 120d,
            initialControlValvePercentOpen: 5d);
}
