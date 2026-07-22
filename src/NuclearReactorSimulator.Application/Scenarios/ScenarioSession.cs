using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>Loaded deterministic scenario session with explicit snapshot and command boundaries.</summary>
public sealed class ScenarioSession
{
    internal ScenarioSession(
        ScenarioDefinition scenario,
        InitialConditionDescriptor initialCondition,
        ControlRoomRuntimeCoordinator coordinator,
        ScenarioCommandDispatcher commandDispatcher,
        ScenarioOperatorActionJournal operatorActions,
        ScenarioPlantControlAuthorityDispatcher plantControlAuthority,
        ScenarioAutomationIntentJournal automationIntents)
    {
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        InitialCondition = initialCondition ?? throw new ArgumentNullException(nameof(initialCondition));
        Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        CommandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        OperatorActions = operatorActions ?? throw new ArgumentNullException(nameof(operatorActions));
        PlantControlAuthority = plantControlAuthority ?? throw new ArgumentNullException(nameof(plantControlAuthority));
        AutomationIntents = automationIntents ?? throw new ArgumentNullException(nameof(automationIntents));
    }

    public ScenarioDefinition Scenario { get; }

    public InitialConditionDescriptor InitialCondition { get; }

    public ControlRoomRuntimeCoordinator Coordinator { get; }

    public IControlRoomSnapshotSource SnapshotSource => Coordinator;

    public IControlRoomCommandDispatcher CommandDispatcher { get; }

    public IPlantControlAuthorityDispatcher PlantControlAuthority { get; }

    public ScenarioOperatorActionJournal OperatorActions { get; }

    public ScenarioAutomationIntentJournal AutomationIntents { get; }
}
