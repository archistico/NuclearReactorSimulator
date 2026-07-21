using System.Collections.ObjectModel;
using System.Globalization;

namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed class ControlRoomTrendSeriesSnapshot
{
    public ControlRoomTrendSeriesSnapshot(
        string sourceId,
        string title,
        string unit,
        string provenance,
        IEnumerable<ControlRoomTrendPointSnapshot> points,
        double? minimum,
        double? maximum,
        double? current,
        string sparklineText)
    {
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        Points = new ReadOnlyCollection<ControlRoomTrendPointSnapshot>(
            (points ?? throw new ArgumentNullException(nameof(points))).ToArray());
        Minimum = minimum;
        Maximum = maximum;
        Current = current;
        SparklineText = sparklineText ?? throw new ArgumentNullException(nameof(sparklineText));
    }

    public string SourceId { get; }
    public string Title { get; }
    public string Unit { get; }
    public string Provenance { get; }
    public IReadOnlyList<ControlRoomTrendPointSnapshot> Points { get; }
    public double? Minimum { get; }
    public double? Maximum { get; }
    public double? Current { get; }
    public string SparklineText { get; }
    public string CurrentText => Format(Current);
    public string MinimumText => Format(Minimum);
    public string MaximumText => Format(Maximum);
    public string MinimumLabel => $"min {MinimumText}";
    public string MaximumLabel => $"max {MaximumText}";
    public string SampleCountText => $"{Points.Count} samples";

    private string Format(double? value)
        => value.HasValue && double.IsFinite(value.Value)
            ? $"{value.Value.ToString("0.###", CultureInfo.InvariantCulture)} {Unit}".TrimEnd()
            : "—";
}
