using NuclearReactorSimulator.Domain.Physics.Quantities;
using NuclearReactorSimulator.Domain.Physics.Reactor;
using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;

/// <summary>
/// Maps rod position to an explicit control-rod reactivity contribution.
/// This is an integral worth approximation, not neutron kinetics.
/// </summary>
public sealed class ControlRodWorthSolver
{
    public ReactivityContribution Evaluate(ControlRodDefinition definition, ControlRodState state)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(state);

        if (!string.Equals(definition.Id, state.RodId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Control rod '{definition.Id}' received state for rod '{state.RodId}'.",
                nameof(state));
        }

        var withdrawal = state.Position.FractionWithdrawn;
        var interpolation = definition.WorthCurveKind switch
        {
            ControlRodWorthCurveKind.Linear => withdrawal,
            ControlRodWorthCurveKind.SmoothStep => withdrawal * withdrawal * (3d - (2d * withdrawal)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(definition),
                definition.WorthCurveKind,
                "Unknown control-rod worth curve kind."),
        };

        var inserted = definition.FullyInsertedReactivity.DeltaKOverK;
        var withdrawn = definition.FullyWithdrawnReactivity.DeltaKOverK;
        var value = Reactivity.FromDeltaKOverK(inserted + ((withdrawn - inserted) * interpolation));

        return new ReactivityContribution(
            $"control-rods/{definition.Id}",
            ReactivityContributionKind.ControlRods,
            value);
    }
}
