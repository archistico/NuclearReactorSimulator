using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Immutable measured-state boundary intended for M5 controllers and later UI/view models.</summary>
public sealed class MeasuredSignalFrame
{
    public MeasuredSignalFrame(
        InstrumentationSystemDefinition definition,
        IEnumerable<MeasuredSignal> signals)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(signals);

        var canonical = signals
            .Select(signal => signal ?? throw new ArgumentException("Measured-signal frames cannot contain null entries.", nameof(signals)))
            .OrderBy(static signal => signal.ChannelId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Channels.Select(static channel => channel.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static signal => signal.ChannelId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Measured-signal frame must contain exactly one signal per instrument channel.", nameof(signals));
        }

        Signals = new ReadOnlyCollection<MeasuredSignal>(canonical);
    }

    public InstrumentationSystemDefinition Definition { get; }

    public IReadOnlyList<MeasuredSignal> Signals { get; }

    public MeasuredSignal GetSignal(string channelId)
        => Signals.FirstOrDefault(signal => string.Equals(signal.ChannelId, channelId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown measured signal '{channelId}'.");
}
