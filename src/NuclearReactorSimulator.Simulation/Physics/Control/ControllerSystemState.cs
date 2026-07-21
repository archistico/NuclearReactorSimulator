using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed class ControllerSystemState
{
    public ControllerSystemState(ControlSystemDefinition definition, IEnumerable<ControllerChannelState> controllers)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(controllers);
        var canonical = controllers
            .Select(item => item ?? throw new ArgumentException("Controller state cannot contain null entries.", nameof(controllers)))
            .OrderBy(static item => item.ControllerId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Controllers.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.ControllerId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Controller state must contain exactly one state per controller.", nameof(controllers));
        }

        Controllers = new ReadOnlyCollection<ControllerChannelState>(canonical);
    }

    public ControlSystemDefinition Definition { get; }
    public IReadOnlyList<ControllerChannelState> Controllers { get; }

    public ControllerChannelState GetController(string id)
        => Controllers.FirstOrDefault(item => string.Equals(item.ControllerId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown controller state '{id}'.");

    public static ControllerSystemState CreateUninitialized(ControlSystemDefinition definition)
        => new(
            definition,
            definition.Controllers.Select(static controller => new ControllerChannelState(
                controller.Id,
                false,
                ControllerMode.Manual,
                0d,
                0d,
                controller.OutputRange.Clamp(0d))));
}
