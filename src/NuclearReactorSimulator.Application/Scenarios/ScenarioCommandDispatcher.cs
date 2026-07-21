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

    public ScenarioCommandDispatcher(ScenarioDefinition scenario, IControlRoomCommandDispatcher inner)
    {
        _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
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
    }
}
