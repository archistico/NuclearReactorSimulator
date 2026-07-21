using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control;

namespace NuclearReactorSimulator.Simulation.Physics.Control;

public sealed class ActuatorSystemState
{
    public ActuatorSystemState(ActuatorSystemDefinition definition, IEnumerable<ActuatorCommandState> actuators)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(actuators);
        var canonical = actuators
            .Select(item => item ?? throw new ArgumentException("Actuator state cannot contain null entries.", nameof(actuators)))
            .OrderBy(static item => item.ActuatorId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Actuators.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.ActuatorId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Actuator command state must contain exactly one state per actuator.", nameof(actuators));
        }
        Actuators = new ReadOnlyCollection<ActuatorCommandState>(canonical);
    }

    public ActuatorSystemDefinition Definition { get; }
    public IReadOnlyList<ActuatorCommandState> Actuators { get; }

    public static ActuatorSystemState CreateInitial(ActuatorSystemDefinition definition)
        => new(definition, definition.Actuators.Select(static actuator => new ActuatorCommandState(actuator.Id, actuator.InputRange.Clamp(0d))));
}
