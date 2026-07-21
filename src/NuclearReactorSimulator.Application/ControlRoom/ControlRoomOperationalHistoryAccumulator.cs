namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// M6.6 presentation-only bounded history. It consumes immutable ControlRoomSnapshot instances and logical alarm sequences;
/// it never consults wall clock time or authoritative Simulation state.
/// </summary>
public sealed class ControlRoomOperationalHistoryAccumulator
{
    private const int DefaultSampleCapacity = 240;
    private const int DefaultEventCapacity = 250;
    private static readonly char[] SparkBlocks = new[] { '▁', '▂', '▃', '▄', '▅', '▆', '▇', '█' };

    private readonly int _sampleCapacity;
    private readonly int _eventCapacity;
    private readonly IReadOnlyList<ControlRoomTrendSourceDescriptor> _sources;
    private readonly Dictionary<string, List<ControlRoomTrendPointSnapshot>> _points;
    private readonly SortedDictionary<long, ControlRoomAlarmEventPresentationSnapshot> _events = new();

    public ControlRoomOperationalHistoryAccumulator(
        IEnumerable<string>? enabledSourceIds = null,
        int sampleCapacity = DefaultSampleCapacity,
        int eventCapacity = DefaultEventCapacity,
        int maximumVisibleTrendSeries = 12)
    {
        if (sampleCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCapacity));
        }
        if (eventCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventCapacity));
        }
        if (maximumVisibleTrendSeries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVisibleTrendSeries));
        }

        var ids = (enabledSourceIds ?? ControlRoomTrendSourceCatalog.DefaultEnabledSourceIds)
            .Select(id => string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Trend source ids cannot be empty.", nameof(enabledSourceIds)) : id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (ids.Length > maximumVisibleTrendSeries)
        {
            throw new ArgumentException("Configured trend series exceed the presentation budget.", nameof(enabledSourceIds));
        }

        _sources = ids.Select(ControlRoomTrendSourceCatalog.Get).ToArray();
        _sampleCapacity = sampleCapacity;
        _eventCapacity = eventCapacity;
        _points = _sources.ToDictionary(static source => source.Id, static _ => new List<ControlRoomTrendPointSnapshot>(), StringComparer.Ordinal);
        Current = BuildSnapshot();
    }

    public ControlRoomOperationalHistorySnapshot Current { get; private set; }

    public ControlRoomOperationalHistorySnapshot Observe(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.RunState == ControlRoomRunState.ShellOnly)
        {
            return Current;
        }

        foreach (var source in _sources)
        {
            var series = _points[source.Id];
            var point = new ControlRoomTrendPointSnapshot(snapshot.LogicalStep, ResolveValue(snapshot, source.Id));
            if (series.Count > 0 && snapshot.LogicalStep < series[^1].LogicalStep)
            {
                throw new InvalidOperationException("Control-room trend snapshots must be observed in non-decreasing logical-step order.");
            }
            if (series.Count > 0 && series[^1].LogicalStep == snapshot.LogicalStep)
            {
                series[^1] = point;
            }
            else
            {
                series.Add(point);
                TrimFront(series, _sampleCapacity);
            }
        }

        foreach (var alarmEvent in snapshot.AlarmEvents.Events)
        {
            if (!_events.ContainsKey(alarmEvent.Sequence))
            {
                _events.Add(alarmEvent.Sequence, alarmEvent);
            }
        }
        while (_events.Count > _eventCapacity)
        {
            _events.Remove(_events.Keys.First());
        }

        Current = BuildSnapshot();
        return Current;
    }

    private ControlRoomOperationalHistorySnapshot BuildSnapshot()
    {
        var trends = _sources.Select(source => BuildSeries(source, _points[source.Id])).ToArray();
        var events = _events.Values.OrderByDescending(static item => item.Sequence).ToArray();
        return new ControlRoomOperationalHistorySnapshot(trends, events);
    }

    private static ControlRoomTrendSeriesSnapshot BuildSeries(
        ControlRoomTrendSourceDescriptor source,
        IReadOnlyList<ControlRoomTrendPointSnapshot> points)
    {
        var finite = points
            .Where(static point => point.Value.HasValue && double.IsFinite(point.Value.Value))
            .Select(static point => point.Value!.Value)
            .ToArray();
        double? minimum = finite.Length == 0 ? null : finite.Min();
        double? maximum = finite.Length == 0 ? null : finite.Max();
        double? current = points.Count == 0 ? null : points[^1].Value;
        return new ControlRoomTrendSeriesSnapshot(
            source.Id,
            source.Title,
            source.Unit,
            source.Provenance,
            points.ToArray(),
            minimum,
            maximum,
            current,
            BuildSparkline(points, minimum, maximum));
    }

    private static string BuildSparkline(
        IReadOnlyList<ControlRoomTrendPointSnapshot> points,
        double? minimum,
        double? maximum)
    {
        if (points.Count == 0)
        {
            return "—";
        }

        var start = Math.Max(0, points.Count - 64);
        var chars = new char[points.Count - start];
        for (var index = start; index < points.Count; index++)
        {
            var value = points[index].Value;
            if (!value.HasValue || !double.IsFinite(value.Value) || !minimum.HasValue || !maximum.HasValue)
            {
                chars[index - start] = '·';
                continue;
            }

            if (maximum.Value <= minimum.Value)
            {
                chars[index - start] = '▄';
                continue;
            }

            var normalized = (value.Value - minimum.Value) / (maximum.Value - minimum.Value);
            var blockIndex = Math.Clamp((int)Math.Round(normalized * (SparkBlocks.Length - 1)), 0, SparkBlocks.Length - 1);
            chars[index - start] = SparkBlocks[blockIndex];
        }
        return new string(chars);
    }

    private static double? ResolveValue(ControlRoomSnapshot snapshot, string sourceId)
        => sourceId switch
        {
            ControlRoomTrendSourceCatalog.ReactorThermalPower => snapshot.ReactorCore.ReactorThermalPower.NumericValue,
            ControlRoomTrendSourceCatalog.PrimaryFeedwaterFlow => snapshot.PrimaryCircuit.TotalFeedwaterFlow.NumericValue,
            ControlRoomTrendSourceCatalog.PrimarySteamExportFlow => snapshot.PrimaryCircuit.TotalSteamExportFlow.NumericValue,
            ControlRoomTrendSourceCatalog.TurbineShaftPower => snapshot.TurbineSecondary.TotalTurbineShaftPower.NumericValue,
            ControlRoomTrendSourceCatalog.GrossElectricalOutput => snapshot.Electrical.GrossElectricalOutput.NumericValue,
            ControlRoomTrendSourceCatalog.UnacknowledgedAlarms => snapshot.UnacknowledgedAlarmCount,
            _ => throw new KeyNotFoundException($"Unknown control-room trend source '{sourceId}'."),
        };

    private static void TrimFront<T>(List<T> items, int capacity)
    {
        if (items.Count > capacity)
        {
            items.RemoveRange(0, items.Count - capacity);
        }
    }
}
