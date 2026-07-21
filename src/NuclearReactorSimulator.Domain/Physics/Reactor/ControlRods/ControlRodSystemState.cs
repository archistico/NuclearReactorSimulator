using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Reactor.ControlRods;

/// <summary>
/// Canonical immutable collection of control-rod operational states.
/// </summary>
public sealed record ControlRodSystemState
{
    public ControlRodSystemState(IEnumerable<ControlRodState> rods)
    {
        ArgumentNullException.ThrowIfNull(rods);

        var canonical = rods
            .Select(static rod => rod ?? throw new ArgumentException("Control-rod states cannot contain null entries.", "rods"))
            .OrderBy(static rod => rod.RodId, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Length == 0)
        {
            throw new ArgumentException("A control-rod system state must contain at least one rod.", "rods");
        }

        if (canonical.Select(static rod => rod.RodId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("A control-rod system state cannot contain duplicate rod ids.", "rods");
        }

        Rods = new ReadOnlyCollection<ControlRodState>(canonical);
    }

    public IReadOnlyList<ControlRodState> Rods { get; }

    public ControlRodState GetRod(string id)
        => Rods.FirstOrDefault(rod => string.Equals(rod.RodId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown control-rod state '{id}'.");
}
