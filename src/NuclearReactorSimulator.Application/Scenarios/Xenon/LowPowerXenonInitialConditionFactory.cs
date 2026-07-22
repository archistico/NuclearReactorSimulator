using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

namespace NuclearReactorSimulator.Application.Scenarios.Xenon;

/// <summary>
/// M9.3 low-power manoeuvring seed with non-zero poison memory. It seeds only canonical M2 state; no scenario-owned
/// poison integrator, scripted xenon curve or forced reactor-power trajectory is introduced.
/// </summary>
public sealed class LowPowerXenonInitialConditionFactory : IVersionedInitialConditionFactory
{
    public InitialConditionDescriptor Descriptor { get; } = new(
        AdvancedXenonScenarioPack.LowPowerInitialCondition,
        "Poisoned Low-Power Operation v1",
        "Educational low-power condition with established circulation, turbine/grid isolation and explicit iodine/xenon inventory memory for operator manoeuvring practice.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            NeutronPopulation.FromRelative(0.03d),
            mainCirculationRunning: true,
            initialRodPosition: ControlRodPosition.FromPercentWithdrawn(58d),
            initialPrimaryTemperatureCelsius: 110d,
            turbineStartupLineup: false,
            iodineXenonDefinition: AdvancedXenonModelConfiguration.Definition,
            initialIodineXenonState: new IodineXenonState(
                IodineInventory.FromRelative(0.45d),
                XenonInventory.FromRelative(0.35d)));
}
