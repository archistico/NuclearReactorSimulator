using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Immutable M7.1 training-scenario definition. It selects one exact versioned initial condition and declares presentation-
/// level metadata/objectives plus the operator command kinds permitted by the scenario. It never encodes forced outcomes.
/// </summary>
public sealed class ScenarioDefinition
{
    private readonly IReadOnlyList<ScenarioObjectiveDefinition> _objectives;
    private readonly IReadOnlySet<ControlRoomCommandKind> _allowedOperatorActions;

    public ScenarioDefinition(
        string scenarioId,
        string title,
        string description,
        InitialConditionReference initialCondition,
        IEnumerable<ScenarioObjectiveDefinition>? objectives = null,
        IEnumerable<ControlRoomCommandKind>? allowedOperatorActions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenarioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ScenarioId = scenarioId;
        Title = title;
        Description = description;
        InitialCondition = initialCondition ?? throw new ArgumentNullException(nameof(initialCondition));

        var objectiveArray = (objectives ?? Array.Empty<ScenarioObjectiveDefinition>()).ToArray();
        if (objectiveArray.Any(static objective => objective is null))
        {
            throw new ArgumentException("Scenario objectives cannot contain null entries.", nameof(objectives));
        }
        if (objectiveArray.Select(static objective => objective.ObjectiveId).Distinct(StringComparer.Ordinal).Count() != objectiveArray.Length)
        {
            throw new ArgumentException("Scenario objective IDs must be unique.", nameof(objectives));
        }

        var actionSet = new HashSet<ControlRoomCommandKind>(allowedOperatorActions ?? Array.Empty<ControlRoomCommandKind>());
        if (actionSet.Any(static action => !Enum.IsDefined(action)))
        {
            throw new ArgumentOutOfRangeException(nameof(allowedOperatorActions), "Scenario operator actions must use defined command kinds.");
        }
        if (actionSet.Any(IsRuntimeHostCommand))
        {
            throw new ArgumentException(
                "Run, pause and single-step are runtime-host controls and cannot be declared as scenario operator actions.",
                nameof(allowedOperatorActions));
        }

        _objectives = Array.AsReadOnly(objectiveArray);
        _allowedOperatorActions = actionSet;
    }

    public string ScenarioId { get; }

    public string Title { get; }

    public string Description { get; }

    public InitialConditionReference InitialCondition { get; }

    public IReadOnlyList<ScenarioObjectiveDefinition> Objectives => _objectives;

    public IReadOnlySet<ControlRoomCommandKind> AllowedOperatorActions => _allowedOperatorActions;

    internal static bool IsRuntimeHostCommand(ControlRoomCommandKind kind)
        => kind is ControlRoomCommandKind.Run or ControlRoomCommandKind.Pause or ControlRoomCommandKind.SingleStep;
}
