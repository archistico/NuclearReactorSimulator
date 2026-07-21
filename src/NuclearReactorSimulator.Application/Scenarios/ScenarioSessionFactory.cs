using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// M7.1 load/start boundary. Loading resolves an exact initial-condition version, creates a fresh runtime, and starts the
/// session paused. No scenario metadata may mutate the returned physical state to force an objective or outcome.
/// </summary>
public sealed class ScenarioSessionFactory
{
    private readonly VersionedInitialConditionRegistry _initialConditions;
    private readonly ControlRoomRuntimeExecutionBudget _executionBudget;

    public ScenarioSessionFactory(
        VersionedInitialConditionRegistry initialConditions,
        ControlRoomRuntimeExecutionBudget? executionBudget = null)
    {
        _initialConditions = initialConditions ?? throw new ArgumentNullException(nameof(initialConditions));
        _executionBudget = executionBudget ?? ControlRoomRuntimeExecutionBudget.DesktopDefault;
    }

    public ScenarioSession Load(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var initialConditionFactory = _initialConditions.Resolve(scenario.InitialCondition);
        var engine = initialConditionFactory.CreateRuntimeEngine()
            ?? throw new InvalidOperationException("Initial-condition factories must return a runtime engine.");
        var coordinator = new ControlRoomRuntimeCoordinator(
            engine,
            ControlRoomRunState.Paused,
            _executionBudget);
        var commandDispatcher = new ScenarioCommandDispatcher(scenario, coordinator);
        return new ScenarioSession(scenario, initialConditionFactory.Descriptor, coordinator, commandDispatcher);
    }
}
