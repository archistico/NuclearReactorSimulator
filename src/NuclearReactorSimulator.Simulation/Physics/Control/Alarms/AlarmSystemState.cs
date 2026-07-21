using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.Alarms;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

/// <summary>M5.6 operator-annunciator memory only. It owns no physical trip, actuator or conserved plant state.</summary>
public sealed class AlarmSystemState
{
    public AlarmSystemState(AlarmSystemDefinition definition, IEnumerable<AlarmChannelState> channels, long nextEventSequence = 1)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ArgumentNullException.ThrowIfNull(channels);
        if (nextEventSequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(nextEventSequence), nextEventSequence, "Next alarm-event sequence must be positive.");
        }

        var canonical = channels.Select(item => item ?? throw new ArgumentException("Alarm channel state cannot contain null entries.", nameof(channels)))
            .OrderBy(static item => item.AlarmId, StringComparer.Ordinal)
            .ToArray();
        var expected = definition.Alarms.Select(static item => item.Id).OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        var actual = canonical.Select(static item => item.AlarmId).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length || !expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            throw new ArgumentException("Alarm state must contain exactly one channel state per alarm definition.", nameof(channels));
        }

        Channels = new ReadOnlyCollection<AlarmChannelState>(canonical);
        NextEventSequence = nextEventSequence;
    }

    public AlarmSystemDefinition Definition { get; }
    public IReadOnlyList<AlarmChannelState> Channels { get; }
    public long NextEventSequence { get; }

    public AlarmChannelState GetChannel(string alarmId)
        => Channels.FirstOrDefault(item => string.Equals(item.AlarmId, alarmId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown alarm channel state '{alarmId}'.");

    public static AlarmSystemState CreateInitial(AlarmSystemDefinition definition)
        => new(definition, definition.Alarms.Select(static alarm => new AlarmChannelState(alarm.Id, false, false, false, false, null)));
}
