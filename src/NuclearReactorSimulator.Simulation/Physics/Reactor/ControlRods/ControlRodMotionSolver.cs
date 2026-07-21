using NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

namespace NuclearReactorSimulator.Simulation.Physics.Reactor.ControlRods;

/// <summary>
/// Deterministically advances one rod according to its persistent motion command and mechanical limits.
/// </summary>
public sealed class ControlRodMotionSolver
{
    public ControlRodState Advance(
        ControlRodDefinition definition,
        ControlRodState state,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(state);

        ValidateIdentity(definition, state);

        if (elapsed <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), elapsed, "Control-rod integration interval must be greater than zero.");
        }

        if (state.Motion == ControlRodMotion.Hold)
        {
            return state;
        }

        var direction = state.Motion == ControlRodMotion.Withdraw ? 1d : -1d;
        var requestedPosition = state.Position.FractionWithdrawn
            + (direction * definition.TravelRate.FractionPerSecond * elapsed.TotalSeconds);
        var clampedPosition = Math.Clamp(requestedPosition, 0d, 1d);
        var position = ControlRodPosition.FromFractionWithdrawn(clampedPosition);
        var reachedLimit = clampedPosition is 0d or 1d;

        return new ControlRodState(
            state.RodId,
            position,
            reachedLimit ? ControlRodMotion.Hold : state.Motion);
    }

    private static void ValidateIdentity(ControlRodDefinition definition, ControlRodState state)
    {
        if (!string.Equals(definition.Id, state.RodId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Control rod '{definition.Id}' received state for rod '{state.RodId}'.",
                nameof(state));
        }
    }
}
