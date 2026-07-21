using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>Loaded deterministic scenario session with explicit snapshot and command boundaries.</summary>
public sealed class ScenarioSession
{
    internal ScenarioSession(
        ScenarioDefinition scenario,
        InitialConditionDescriptor initialCondition,
        ControlRoomRuntimeCoordinator coordinator,
        ScenarioCommandDispatcher commandDispatcher)
    {
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        InitialCondition = initialCondition ?? throw new ArgumentNullException(nameof(initialCondition));
        Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        CommandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
    }

    public ScenarioDefinition Scenario { get; }

    public InitialConditionDescriptor InitialCondition { get; }

    public ControlRoomRuntimeCoordinator Coordinator { get; }

    public IControlRoomSnapshotSource SnapshotSource => Coordinator;

    public IControlRoomCommandDispatcher CommandDispatcher { get; }
}
