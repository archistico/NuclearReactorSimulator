using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Domain.Physics.Instrumentation;

/// <summary>Canonical M5.1 composition of measured-signal channels over full-plant true state.</summary>
public sealed class InstrumentationSystemDefinition
{
    public InstrumentationSystemDefinition(string id, IEnumerable<InstrumentChannelDefinition> channels)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Instrumentation-system id cannot be empty or whitespace.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(channels);
        var canonical = channels
            .Select(channel => channel ?? throw new ArgumentException("Instrumentation channels cannot contain null entries.", nameof(channels)))
            .OrderBy(static channel => channel.Id, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Length == 0)
        {
            throw new ArgumentException("An instrumentation system must contain at least one channel.", nameof(channels));
        }

        if (canonical.Select(static channel => channel.Id).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Instrument-channel ids must be unique.", nameof(channels));
        }

        Id = id.Trim();
        Channels = new ReadOnlyCollection<InstrumentChannelDefinition>(canonical);
    }

    public string Id { get; }

    public IReadOnlyList<InstrumentChannelDefinition> Channels { get; }

    public InstrumentChannelDefinition GetChannel(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Instrument-channel id cannot be empty or whitespace.", nameof(id));
        }

        return Channels.FirstOrDefault(channel => string.Equals(channel.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown instrument channel '{id}'.");
    }
}
