using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Scenario-level command gate. Runtime host controls are always forwarded; physical/operator commands must be explicitly
/// allowed by scenario metadata. The gate never interprets or executes physics.
/// </summary>
public sealed class ScenarioCommandDispatcher : IControlRoomCommandDispatcher
{
    private readonly ScenarioDefinition _scenario;
    private readonly IControlRoomCommandDispatcher _inner;
    private readonly ScenarioOperatorActionJournal? _operatorActions;
    private readonly Func<long>? _logicalStepSource;

    public ScenarioCommandDispatcher(ScenarioDefinition scenario, IControlRoomCommandDispatcher inner)
        : this(scenario, inner, null, null)
    {
    }

    public ScenarioCommandDispatcher(
        ScenarioDefinition scenario,
        IControlRoomCommandDispatcher inner,
        ScenarioOperatorActionJournal? operatorActions,
        Func<long>? logicalStepSource)
    {
        _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _operatorActions = operatorActions;
        _logicalStepSource = logicalStepSource;

        if (operatorActions is not null && logicalStepSource is null)
        {
            throw new ArgumentNullException(nameof(logicalStepSource), "An operator-action journal requires a deterministic logical-step source.");
        }
    }

    public void Dispatch(ControlRoomCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!ScenarioDefinition.IsRuntimeHostCommand(command.Kind)
            && !_scenario.AllowedOperatorActions.Contains(command.Kind))
        {
            throw new InvalidOperationException(
                $"Operator action '{command.Kind}' is not allowed by scenario '{_scenario.ScenarioId}'.");
        }

        _inner.Dispatch(command);

        if (!ScenarioDefinition.IsRuntimeHostCommand(command.Kind) && _operatorActions is not null)
        {
            _operatorActions.RecordAccepted(_logicalStepSource!(), command);
        }
    }
}
