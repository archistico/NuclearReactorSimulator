using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed class ControllerInputs
{
    public ControllerInputs(ControlSystemDefinition definition, IEnumerable<ControllerInput> controllers)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(controllers);
        var canonical = controllers
            .Select(item => item ?? throw new ArgumentException("Controller inputs cannot contain null entries.", nameof(controllers)))
            .OrderBy(static item => item.ControllerId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Controllers.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.ControllerId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Controller inputs must contain exactly one input per controller.", nameof(controllers));
        }

        Controllers = new ReadOnlyCollection<ControllerInput>(canonical);
    }

    public ControlSystemDefinition Definition { get; }
    public IReadOnlyList<ControllerInput> Controllers { get; }

    public ControllerInput GetController(string id)
        => Controllers.FirstOrDefault(item => string.Equals(item.ControllerId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown controller input '{id}'.");
}
