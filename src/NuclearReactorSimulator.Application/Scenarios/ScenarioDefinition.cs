using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Faults;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Immutable versioned scenario definition. M7.1 owns exact initial-condition/objective/action metadata; M8.1 extends the
/// same document with explicit deterministic fault declarations. Scenario data never encodes forced physical outcomes.
/// </summary>
public sealed class ScenarioDefinition
{
    private readonly IReadOnlyList<ScenarioObjectiveDefinition> _objectives;
    private readonly IReadOnlySet<ControlRoomCommandKind> _allowedOperatorActions;
    private readonly IReadOnlyList<ScenarioFaultDefinition> _faults;

    public ScenarioDefinition(
        string scenarioId,
        string title,
        string description,
        InitialConditionReference initialCondition,
        IEnumerable<ScenarioObjectiveDefinition>? objectives = null,
        IEnumerable<ControlRoomCommandKind>? allowedOperatorActions = null,
        IEnumerable<ScenarioFaultDefinition>? faults = null)
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

        var faultArray = (faults ?? Array.Empty<ScenarioFaultDefinition>()).ToArray();
        if (faultArray.Any(static fault => fault is null))
        {
            throw new ArgumentException("Scenario faults cannot contain null entries.", nameof(faults));
        }
        if (faultArray.Select(static fault => fault.FaultId).Distinct(StringComparer.Ordinal).Count() != faultArray.Length)
        {
            throw new ArgumentException("Scenario fault IDs must be unique.", nameof(faults));
        }
        faultArray = faultArray.OrderBy(static fault => fault.FaultId, StringComparer.Ordinal).ToArray();

        _objectives = Array.AsReadOnly(objectiveArray);
        _allowedOperatorActions = actionSet;
        _faults = Array.AsReadOnly(faultArray);
    }

    public string ScenarioId { get; }

    public string Title { get; }

    public string Description { get; }

    public InitialConditionReference InitialCondition { get; }

    public IReadOnlyList<ScenarioObjectiveDefinition> Objectives => _objectives;

    public IReadOnlySet<ControlRoomCommandKind> AllowedOperatorActions => _allowedOperatorActions;

    public IReadOnlyList<ScenarioFaultDefinition> Faults => _faults;

    internal static bool IsRuntimeHostCommand(ControlRoomCommandKind kind)
        => kind is ControlRoomCommandKind.Run or ControlRoomCommandKind.Pause or ControlRoomCommandKind.SingleStep;
}
