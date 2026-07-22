using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.PreStartup;
using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Domain.Physics.Reactor.IodineXenon;

namespace NuclearReactorSimulator.Application.Scenarios.Xenon;

/// <summary>
/// M9.3 exact post-shutdown restart seed. The poison inventories are versioned initial-condition data representing prior
/// operating history; subsequent iodine/xenon evolution remains exclusively owned by the canonical M2.8 solver.
/// </summary>
public sealed class XenonRestartInitialConditionFactory : IVersionedInitialConditionFactory
{
    public InitialConditionDescriptor Descriptor { get; } = new(
        AdvancedXenonScenarioPack.RestartInitialCondition,
        "Post-Shutdown Xenon Restart Window v1",
        "Educational post-shutdown restart window with circulation established, turbine/grid isolated, residual source-range power and explicit I-135/Xe-135 inventory memory from prior operation.");

    public IControlRoomRuntimeEngine CreateRuntimeEngine()
        => ColdShutdownInitialConditionFactory.CreateRuntimeEngineForOperationalSeed(
            NeutronPopulation.FromRelative(0.005d),
            mainCirculationRunning: true,
            initialRodPosition: ControlRodPosition.FromPercentWithdrawn(65d),
            initialPrimaryTemperatureCelsius: 100d,
            turbineStartupLineup: false,
            iodineXenonDefinition: AdvancedXenonModelConfiguration.Definition,
            initialIodineXenonState: new IodineXenonState(
                IodineInventory.FromRelative(1.0d),
                XenonInventory.FromRelative(0.5d)));
}
