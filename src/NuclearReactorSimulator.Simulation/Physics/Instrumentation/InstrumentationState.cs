using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Instrumentation;

/// <summary>Committed M5.1 instrumentation dynamics, separate from true plant state.</summary>
public sealed class InstrumentationState
{
    public InstrumentationState(
        InstrumentationSystemDefinition definition,
        IEnumerable<InstrumentationChannelState> channels)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(channels);

        var canonical = channels
            .Select(channel => channel ?? throw new ArgumentException("Instrumentation state cannot contain null channel states.", nameof(channels)))
            .OrderBy(static channel => channel.ChannelId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Channels.Select(static channel => channel.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static channel => channel.ChannelId).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                $"Instrumentation state must contain exactly one state per channel. Expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].",
                nameof(channels));
        }

        Channels = new ReadOnlyCollection<InstrumentationChannelState>(canonical);
    }

    public InstrumentationSystemDefinition Definition { get; }

    public IReadOnlyList<InstrumentationChannelState> Channels { get; }

    public InstrumentationChannelState GetChannel(string id)
        => Channels.FirstOrDefault(channel => string.Equals(channel.ChannelId, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown instrumentation channel state '{id}'.");

    public static InstrumentationState CreateUninitialized(InstrumentationSystemDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new InstrumentationState(
            definition,
            definition.Channels.Select(static channel => new InstrumentationChannelState(channel.Id, false, 0d, 0d)));
    }
}
