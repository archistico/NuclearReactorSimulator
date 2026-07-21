using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;
using NuclearReactorSimulator.Simulation.Physics.Reactor;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;

/// <summary>
/// Converts the complete control-rod system state into the M2.1 diagnostic reactivity breakdown.
/// </summary>
public sealed class ControlRodReactivitySolver
{
    private readonly ControlRodSystemDefinition _definition;
    private readonly ControlRodWorthSolver _worthSolver;
    private readonly ReactivityModel _reactivityModel;

    public ControlRodReactivitySolver(ControlRodSystemDefinition definition)
        : this(definition, new ControlRodWorthSolver(), new ReactivityModel())
    {
    }

    internal ControlRodReactivitySolver(
        ControlRodSystemDefinition definition,
        ControlRodWorthSolver worthSolver,
        ReactivityModel reactivityModel)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _worthSolver = worthSolver ?? throw new ArgumentNullException(nameof(worthSolver));
        _reactivityModel = reactivityModel ?? throw new ArgumentNullException(nameof(reactivityModel));
    }

    public ReactivityBreakdownSnapshot Evaluate(ControlRodSystemState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Rods.Count != _definition.Rods.Count)
        {
            throw new ArgumentException("Control-rod state does not cover the complete system definition.", nameof(state));
        }

        var contributions = new ReactivityContribution[_definition.Rods.Count];
        for (var index = 0; index < _definition.Rods.Count; index++)
        {
            var definition = _definition.Rods[index];
            var rodState = state.Rods[index];
            if (!string.Equals(definition.Id, rodState.RodId, StringComparison.Ordinal))
            {
                throw new ArgumentException("Control-rod state ids do not match the canonical system definition.", nameof(state));
            }

            contributions[index] = _worthSolver.Evaluate(definition, rodState);
        }

        return _reactivityModel.Evaluate(contributions);
    }
}
