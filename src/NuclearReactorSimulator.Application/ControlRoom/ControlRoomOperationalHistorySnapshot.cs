using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed class ControlRoomOperationalHistorySnapshot
{
    public ControlRoomOperationalHistorySnapshot(
        IEnumerable<ControlRoomTrendSeriesSnapshot> trendSeries,
        IEnumerable<ControlRoomAlarmEventPresentationSnapshot> events)
    {
        TrendSeries = new ReadOnlyCollection<ControlRoomTrendSeriesSnapshot>(
            (trendSeries ?? throw new ArgumentNullException(nameof(trendSeries))).ToArray());
        Events = new ReadOnlyCollection<ControlRoomAlarmEventPresentationSnapshot>(
            (events ?? throw new ArgumentNullException(nameof(events))).ToArray());
    }

    public static ControlRoomOperationalHistorySnapshot Empty { get; } = new(
        Array.Empty<ControlRoomTrendSeriesSnapshot>(),
        Array.Empty<ControlRoomAlarmEventPresentationSnapshot>());

    public IReadOnlyList<ControlRoomTrendSeriesSnapshot> TrendSeries { get; }
    public IReadOnlyList<ControlRoomAlarmEventPresentationSnapshot> Events { get; }
}
