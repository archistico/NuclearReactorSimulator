using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;

/// <summary>
/// Applies persistent rod/group commands and advances all rods from one immutable system state.
/// Commands are applied in caller order; later commands in the same logical step override earlier commands
/// for rods they target. Rod advancement itself always follows canonical definition order.
/// </summary>
public sealed class ControlRodSystemSolver
{
    private readonly ControlRodSystemDefinition _definition;
    private readonly ControlRodMotionSolver _motionSolver;

    public ControlRodSystemSolver(ControlRodSystemDefinition definition)
        : this(definition, new ControlRodMotionSolver())
    {
    }

    internal ControlRodSystemSolver(
        ControlRodSystemDefinition definition,
        ControlRodMotionSolver motionSolver)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _motionSolver = motionSolver ?? throw new ArgumentNullException(nameof(motionSolver));
    }

    public ControlRodSystemState Step(
        ControlRodSystemState state,
        IEnumerable<ControlRodCommand> commands,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commands);

        ValidateStateCoverage(state);

        var working = state.Rods.ToDictionary(static rod => rod.RodId, StringComparer.Ordinal);

        foreach (var command in commands)
        {
            ApplyCommand(working, command ?? throw new ArgumentException("Control-rod commands cannot contain null entries.", nameof(commands)));
        }

        var advanced = new ControlRodState[_definition.Rods.Count];
        for (var index = 0; index < _definition.Rods.Count; index++)
        {
            var rodDefinition = _definition.Rods[index];
            advanced[index] = _motionSolver.Advance(rodDefinition, working[rodDefinition.Id], elapsed);
        }

        return new ControlRodSystemState(advanced);
    }

    private void ApplyCommand(IDictionary<string, ControlRodState> working, ControlRodCommand command)
    {
        switch (command.TargetKind)
        {
            case ControlRodCommandTargetKind.Rod:
                if (!working.TryGetValue(command.TargetId, out var rodState))
                {
                    throw new KeyNotFoundException($"Unknown control-rod command target '{command.TargetId}'.");
                }

                working[command.TargetId] = new ControlRodState(
                    rodState.RodId,
                    rodState.Position,
                    command.Motion);
                break;

            case ControlRodCommandTargetKind.Group:
                var group = _definition.GetGroup(command.TargetId);
                foreach (var rodId in group.RodIds)
                {
                    var state = working[rodId];
                    working[rodId] = new ControlRodState(state.RodId, state.Position, command.Motion);
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(command),
                    command.TargetKind,
                    "Unknown control-rod command target kind.");
        }
    }

    private void ValidateStateCoverage(ControlRodSystemState state)
    {
        if (state.Rods.Count != _definition.Rods.Count)
        {
            throw new ArgumentException("Control-rod state does not cover the complete system definition.", nameof(state));
        }

        for (var index = 0; index < _definition.Rods.Count; index++)
        {
            if (!string.Equals(_definition.Rods[index].Id, state.Rods[index].RodId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Control-rod state ids do not match the canonical system definition.", nameof(state));
            }
        }
    }
}
