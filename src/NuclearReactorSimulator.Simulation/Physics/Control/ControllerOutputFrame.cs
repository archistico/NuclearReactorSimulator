using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed class ControllerOutputFrame
{
    public ControllerOutputFrame(ControlSystemDefinition definition, IEnumerable<ControllerOutput> outputs)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(outputs);
        var canonical = outputs
            .Select(item => item ?? throw new ArgumentException("Controller outputs cannot contain null entries.", nameof(outputs)))
            .OrderBy(static item => item.ControllerId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Controllers.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.ControllerId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Controller output frame must contain exactly one output per controller.", nameof(outputs));
        }

        Outputs = new ReadOnlyCollection<ControllerOutput>(canonical);
    }

    public ControlSystemDefinition Definition { get; }
    public IReadOnlyList<ControllerOutput> Outputs { get; }

    public ControllerOutput GetOutput(string id)
        => Outputs.FirstOrDefault(item => string.Equals(item.ControllerId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown controller output '{id}'.");
}
