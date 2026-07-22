using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults;
using NuclearReactorSimulator.Application.Scenarios.Faults.ElectricalLoss;
using NuclearReactorSimulator.Application.Scenarios.Faults.Hydraulics;
using NuclearReactorSimulator.Application.Scenarios.Faults.InstrumentationControl;
using NuclearReactorSimulator.Application.Scenarios.Faults.LossOfCoolant;
using NuclearReactorSimulator.Application.Scenarios.Faults.SecondaryTransients;
using NuclearReactorSimulator.Application.Scenarios.Historical;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// M7.1 load/start boundary extended by M8.1 deterministic fault scheduling and M9.5 historical-fidelity gating. Loading
/// resolves an exact initial-condition version, creates a fresh runtime, binds every declared fault type/condition fail-closed,
/// and starts the session paused. Historical-inspired content must pass explicit capability review before runtime creation.
/// Scenario metadata and fault orchestration may act only through registered runtime seams; neither may fabricate outcomes.
/// </summary>
public sealed class ScenarioSessionFactory
{
    private readonly VersionedInitialConditionRegistry _initialConditions;
    private readonly ControlRoomRuntimeExecutionBudget _executionBudget;
    private readonly ScenarioFaultApplicatorRegistry _faultApplicators;
    private readonly ScenarioFaultConditionRegistry _faultConditions;
    private readonly IReadOnlySet<string> _validatedHistoricalModelCapabilities;

    public ScenarioSessionFactory(
        VersionedInitialConditionRegistry initialConditions,
        ControlRoomRuntimeExecutionBudget? executionBudget = null,
        ScenarioFaultApplicatorRegistry? faultApplicators = null,
        ScenarioFaultConditionRegistry? faultConditions = null,
        IEnumerable<string>? validatedHistoricalModelCapabilities = null)
    {
        _initialConditions = initialConditions ?? throw new ArgumentNullException(nameof(initialConditions));
        _executionBudget = executionBudget ?? ControlRoomRuntimeExecutionBudget.DesktopDefault;
        _faultApplicators = faultApplicators ?? new ScenarioFaultApplicatorRegistry(
            ElectricalLossFaultApplicatorFactory.CreateBuiltIns()
                .Concat(HydraulicFaultApplicatorFactory.CreateBuiltIns())
                .Concat(InstrumentationControlFaultApplicatorFactory.CreateBuiltIns())
                .Concat(SecondaryTransientFaultApplicatorFactory.CreateBuiltIns())
                .Concat(LossOfCoolantFaultApplicatorFactory.CreateBuiltIns()));
        _faultConditions = faultConditions ?? ScenarioFaultConditionRegistry.Empty;
        var historicalCapabilities = (validatedHistoricalModelCapabilities ?? HistoricalModelCapabilityIds.ValidatedThroughM94).ToArray();
        if (historicalCapabilities.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Validated historical model capability IDs cannot contain blank entries.", nameof(validatedHistoricalModelCapabilities));
        }
        _validatedHistoricalModelCapabilities = historicalCapabilities.ToHashSet(StringComparer.Ordinal);
    }

    public ScenarioSession Load(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (scenario.HistoricalContext is not null)
        {
            var review = HistoricalScenarioFidelityReviewer.Review(scenario, _validatedHistoricalModelCapabilities);
            if (!review.IsApproved)
            {
                throw new HistoricalScenarioFidelityException(scenario.ScenarioId, review.MissingCapabilities);
            }
        }

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
        var automationIntents = new ScenarioAutomationIntentJournal();
        var plantControlAuthority = new ScenarioPlantControlAuthorityDispatcher(
            coordinator,
            automationIntents,
            () => coordinator.Current.LogicalStep);
        return new ScenarioSession(
            scenario,
            initialConditionFactory.Descriptor,
            coordinator,
            commandDispatcher,
            operatorActions,
            plantControlAuthority,
            automationIntents);
    }
}
