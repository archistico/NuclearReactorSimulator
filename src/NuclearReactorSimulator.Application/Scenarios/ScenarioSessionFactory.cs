using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// M7.1 load/start boundary extended by M8.1 deterministic fault scheduling. Loading resolves an exact initial-condition
/// version, creates a fresh runtime, binds every declared fault type/condition fail-closed, and starts the session paused.
/// Scenario metadata and fault orchestration may act only through registered runtime seams; neither may fabricate outcomes.
/// </summary>
public sealed class ScenarioSessionFactory
{
    private readonly VersionedInitialConditionRegistry _initialConditions;
    private readonly ControlRoomRuntimeExecutionBudget _executionBudget;
    private readonly ScenarioFaultApplicatorRegistry _faultApplicators;
    private readonly ScenarioFaultConditionRegistry _faultConditions;

    public ScenarioSessionFactory(
        VersionedInitialConditionRegistry initialConditions,
        ControlRoomRuntimeExecutionBudget? executionBudget = null,
        ScenarioFaultApplicatorRegistry? faultApplicators = null,
        ScenarioFaultConditionRegistry? faultConditions = null)
    {
        _initialConditions = initialConditions ?? throw new ArgumentNullException(nameof(initialConditions));
        _executionBudget = executionBudget ?? ControlRoomRuntimeExecutionBudget.DesktopDefault;
        _faultApplicators = faultApplicators ?? new ScenarioFaultApplicatorRegistry(
            HydraulicFaultApplicatorFactory.CreateBuiltIns()
                .Concat(InstrumentationControlFaultApplicatorFactory.CreateBuiltIns()));
        _faultConditions = faultConditions ?? ScenarioFaultConditionRegistry.Empty;
    }

    public ScenarioSession Load(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var initialConditionFactory = _initialConditions.Resolve(scenario.InitialCondition);
        IControlRoomRuntimeEngine engine = initialConditionFactory.CreateRuntimeEngine()
            ?? throw new InvalidOperationException("Initial-condition factories must return a runtime engine.");

        if (scenario.Faults.Count > 0)
        {
            var boundApplicators = _faultApplicators.Bind(engine, scenario.Faults);
            engine = new ScenarioFaultRuntimeEngine(
                engine,
                scenario.Faults,
                boundApplicators,
                _faultConditions);
        }

        var coordinator = new ControlRoomRuntimeCoordinator(
            engine,
            ControlRoomRunState.Paused,
            _executionBudget);
        var operatorActions = new ScenarioOperatorActionJournal();
        var commandDispatcher = new ScenarioCommandDispatcher(
            scenario,
            coordinator,
            operatorActions,
            () => coordinator.Current.LogicalStep);
        return new ScenarioSession(
            scenario,
            initialConditionFactory.Descriptor,
            coordinator,
            commandDispatcher,
            operatorActions);
    }
}
