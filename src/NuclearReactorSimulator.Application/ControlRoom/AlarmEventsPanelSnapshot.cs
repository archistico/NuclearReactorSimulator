using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>M6.6 immutable annunciator/event projection. Events contain only those emitted by the represented logical step.</summary>
public sealed class AlarmEventsPanelSnapshot
{
    public AlarmEventsPanelSnapshot(
        IEnumerable<ControlRoomAlarmPresentationSnapshot> alarms,
        IEnumerable<ControlRoomFirstOutGroupPresentationSnapshot> firstOutGroups,
        IEnumerable<ControlRoomAlarmEventPresentationSnapshot> events)
    {
        Alarms = new ReadOnlyCollection<ControlRoomAlarmPresentationSnapshot>(
            (alarms ?? throw new ArgumentNullException(nameof(alarms))).ToArray());
        FirstOutGroups = new ReadOnlyCollection<ControlRoomFirstOutGroupPresentationSnapshot>(
            (firstOutGroups ?? throw new ArgumentNullException(nameof(firstOutGroups))).ToArray());
        Events = new ReadOnlyCollection<ControlRoomAlarmEventPresentationSnapshot>(
            (events ?? throw new ArgumentNullException(nameof(events))).ToArray());
    }

    public static AlarmEventsPanelSnapshot Unavailable { get; } = new(
        Array.Empty<ControlRoomAlarmPresentationSnapshot>(),
        Array.Empty<ControlRoomFirstOutGroupPresentationSnapshot>(),
        Array.Empty<ControlRoomAlarmEventPresentationSnapshot>());

    public IReadOnlyList<ControlRoomAlarmPresentationSnapshot> Alarms { get; }
    public IReadOnlyList<ControlRoomFirstOutGroupPresentationSnapshot> FirstOutGroups { get; }
    public IReadOnlyList<ControlRoomAlarmEventPresentationSnapshot> Events { get; }
    public int AnnunciatedCount => Alarms.Count(static alarm => alarm.IsAnnunciated);
    public int UnacknowledgedCount => Alarms.Count(static alarm => alarm.IsAnnunciated && !alarm.IsAcknowledged);
    public int FirstOutCount => FirstOutGroups.Count(static group => group.FirstOutAlarmId is not null);
}
