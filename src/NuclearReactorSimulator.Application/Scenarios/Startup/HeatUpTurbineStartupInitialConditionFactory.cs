using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Application.Scenarios.Startup;

/// <summary>
/// M7.4 low-power hot/startup-lineup recipe. It reuses the M7.2 canonical object graph, seeds critical low-power kinetics,
/// establishes a warm pressurized primary/steam condition, and prepares the turbine steam path without creating a second
/// valve/rotor owner. The governing control valve remains under the validated M5.4 turbine-speed command seam.
/// </summary>
public sealed class HeatUpTurbineStartupInitialConditionFactory : IVersionedInitialConditionFactory
{
    private static readonly NeutronPopulation LowPowerSeed = NeutronPopulation.FromRelative(0.05d);
    private static readonly ControlRodPosition CriticalRodPosition = ControlRodPosition.FromPercentWithdrawn(50d);

    public InitialConditionDescriptor Descriptor { get; } = new(
        HeatUpTurbineStartupProgram.InitialCondition,
        "Low-Power Steam-Raising / Turbine Startup v1",
        "Warm low-power critical plant with main circulation established, startup steam lineup prepared, turbine stopped and generator isolated/unloaded for M7.4 heat-up and rolling.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            LowPowerSeed,
            mainCirculationRunning: true,
            initialRodPosition: CriticalRodPosition,
            initialPrimaryTemperatureCelsius: 120d,
            turbineStartupLineup: true);
}
