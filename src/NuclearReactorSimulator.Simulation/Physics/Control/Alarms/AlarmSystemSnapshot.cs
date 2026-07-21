using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.Alarms;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

public sealed class AlarmSystemSnapshot
{
    public AlarmSystemSnapshot(
        AlarmSystemDefinition definition,
        IEnumerable<AlarmSnapshot> alarms,
        IEnumerable<AlarmFirstOutGroupSnapshot> firstOutGroups,
        IEnumerable<AlarmEventSnapshot> events)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Alarms = new ReadOnlyCollection<AlarmSnapshot>(alarms.OrderBy(static item => item.AlarmId, StringComparer.Ordinal).ToArray());
        FirstOutGroups = new ReadOnlyCollection<AlarmFirstOutGroupSnapshot>(firstOutGroups.OrderBy(static item => item.GroupId, StringComparer.Ordinal).ToArray());
        Events = new ReadOnlyCollection<AlarmEventSnapshot>(events.OrderBy(static item => item.Sequence).ToArray());
    }

    public AlarmSystemDefinition Definition { get; }
    public IReadOnlyList<AlarmSnapshot> Alarms { get; }
    public IReadOnlyList<AlarmFirstOutGroupSnapshot> FirstOutGroups { get; }
    public IReadOnlyList<AlarmEventSnapshot> Events { get; }
    public int ActiveConditionCount => Alarms.Count(static item => item.ConditionActive);
    public int AnnunciatedCount => Alarms.Count(static item => item.IsAnnunciated);
    public int UnacknowledgedCount => Alarms.Count(static item => item.IsAnnunciated && !item.IsAcknowledged);

    public AlarmSnapshot GetAlarm(string alarmId)
        => Alarms.FirstOrDefault(item => string.Equals(item.AlarmId, alarmId, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown alarm snapshot '{alarmId}'.");
}
