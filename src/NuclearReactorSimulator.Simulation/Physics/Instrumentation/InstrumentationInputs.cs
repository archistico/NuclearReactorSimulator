using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Complete deterministic M5.1 fault/input set with exact one-input-per-channel coverage.</summary>
public sealed class InstrumentationInputs
{
    public InstrumentationInputs(
        InstrumentationSystemDefinition definition,
        IEnumerable<SensorFaultInput> sensorFaults)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(sensorFaults);

        var canonical = sensorFaults
            .Select(input => input ?? throw new ArgumentException("Instrumentation inputs cannot contain null entries.", nameof(sensorFaults)))
            .OrderBy(static input => input.ChannelId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Channels.Select(static channel => channel.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static input => input.ChannelId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Instrumentation inputs must contain exactly one sensor input per channel. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(sensorFaults));
        }

        SensorFaults = new ReadOnlyCollection<SensorFaultInput>(canonical);
    }

    public InstrumentationSystemDefinition Definition { get; }

    public IReadOnlyList<SensorFaultInput> SensorFaults { get; }

    public SensorFaultInput GetSensorFault(string channelId)
        => SensorFaults.FirstOrDefault(input => string.Equals(input.ChannelId, channelId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown instrumentation input '{channelId}'.");

    public static InstrumentationInputs Healthy(InstrumentationSystemDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new InstrumentationInputs(definition, definition.Channels.Select(static channel => SensorFaultInput.Healthy(channel.Id)));
    }
}
