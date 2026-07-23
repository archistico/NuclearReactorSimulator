using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

/// <summary>M10.9.2 circular banded instrument for quantities where a dial improves limit/target awareness.</summary>
public sealed class ControlRoomCircularGauge : Border
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomCircularGauge, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<ControlRoomValueSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<ControlRoomCircularGauge, ControlRoomValueSnapshot?>(nameof(Snapshot));

    public static readonly StyledProperty<ControlRoomInstrumentTrendSnapshot?> TrendProperty =
        AvaloniaProperty.Register<ControlRoomCircularGauge, ControlRoomInstrumentTrendSnapshot?>(nameof(Trend));

    private readonly TextBlock _label;
    private readonly TextBlock _provenance;
    private readonly TextBlock _quality;
    private readonly CircularGaugeTrack _track;
    private readonly TextBlock _value;
    private readonly TextBlock _unit;
    private readonly TextBlock _state;
    private readonly TextBlock _minimum;
    private readonly TextBlock _maximum;
    private readonly TextBlock _scaleStatus;
    private readonly TextBlock _semantics;
    private readonly TextBlock _trend;

    public ControlRoomCircularGauge()
    {
        Padding = new Thickness(14, 12);
        CornerRadius = new CornerRadius(6);
        Background = ControlRoomPalette.SurfaceInset;
        BorderBrush = ControlRoomPalette.Border;
        BorderThickness = new Thickness(1);

        _label = new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = ControlRoomPalette.TextMuted,
        };
        _provenance = Badge();
        _quality = Badge();
        _track = new CircularGaugeTrack { Height = 154, MinWidth = 190 };
        _value = new TextBlock
        {
            FontSize = 25,
            FontWeight = FontWeight.SemiBold,
            FontFamily = new FontFamily("Consolas"),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        };
        _unit = new TextBlock
        {
            FontSize = 10,
            Foreground = ControlRoomPalette.TextMuted,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        };
        _state = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        };
        _minimum = ScaleLabel(Avalonia.Layout.HorizontalAlignment.Left);
        _maximum = ScaleLabel(Avalonia.Layout.HorizontalAlignment.Right);
        _scaleStatus = ScaleLabel(Avalonia.Layout.HorizontalAlignment.Center);
        _semantics = new TextBlock
        {
            FontSize = 9,
            FontFamily = new FontFamily("Consolas"),
            Foreground = ControlRoomPalette.TextMuted,
            TextWrapping = TextWrapping.Wrap,
        };
        _trend = new TextBlock
        {
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = ControlRoomPalette.TextMuted,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        };

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 6 };
        header.Children.Add(_label);
        Grid.SetColumn(_provenance, 1);
        header.Children.Add(_provenance);
        Grid.SetColumn(_quality, 2);
        header.Children.Add(_quality);

        var scaleRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*") };
        scaleRow.Children.Add(_minimum);
        Grid.SetColumn(_scaleStatus, 1);
        scaleRow.Children.Add(_scaleStatus);
        Grid.SetColumn(_maximum, 2);
        scaleRow.Children.Add(_maximum);

        Child = new StackPanel
        {
            Spacing = 4,
            Children = { header, _track, _value, _unit, _state, scaleRow, _semantics, _trend },
        };

        UpdateVisuals();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public ControlRoomValueSnapshot? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public ControlRoomInstrumentTrendSnapshot? Trend
    {
        get => GetValue(TrendProperty);
        set => SetValue(TrendProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LabelProperty || change.Property == SnapshotProperty || change.Property == TrendProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_label is null)
        {
            return;
        }

        var snapshot = Snapshot;
        var trend = Trend ?? ControlRoomInstrumentTrendSnapshot.Unavailable;
        var scale = snapshot?.InstrumentScale;
        var state = snapshot?.State ?? ControlRoomVisualState.Unavailable;

        _label.Text = Label;
        _provenance.Text = snapshot?.ProvenanceText ?? "SOURCE —";
        _quality.Text = snapshot?.QualityText ?? "UNAVAILABLE";
        _quality.Foreground = snapshot?.Quality == ControlRoomInstrumentQuality.Suspect
            ? ControlRoomPalette.Accent(ControlRoomVisualState.Warning)
            : snapshot?.Quality == ControlRoomInstrumentQuality.Unavailable
                ? ControlRoomPalette.Accent(ControlRoomVisualState.Unavailable)
                : ControlRoomPalette.TextMuted;
        _value.Text = snapshot is null || state == ControlRoomVisualState.Unavailable ? "—" : snapshot.ValueText;
        _unit.Text = snapshot?.Unit ?? string.Empty;
        _state.Text = ControlRoomPalette.StateText(state);
        _state.Foreground = ControlRoomPalette.Accent(state);
        _minimum.Text = scale is null ? "min —" : $"{scale.Minimum:0.###}";
        _maximum.Text = scale is null ? "max —" : $"{scale.Maximum:0.###}";
        _scaleStatus.Text = snapshot?.ScaleStatusText ?? "SCALE —";
        _scaleStatus.Foreground = snapshot?.IsOffScale == true
            ? ControlRoomPalette.Accent(ControlRoomVisualState.Warning)
            : ControlRoomPalette.TextMuted;
        _semantics.Text = snapshot?.ScaleSemanticsText ?? "RANGES —";
        _trend.Text = trend.Direction == ControlRoomInstrumentTrendDirection.Unavailable
            ? "TREND —"
            : $"{trend.ArrowText} {trend.DirectionText} · {trend.RateText}";

        _track.Snapshot = snapshot;
        _track.InvalidateVisual();
    }

    private static TextBlock Badge() => new()
    {
        FontSize = 9,
        FontWeight = FontWeight.SemiBold,
        Foreground = ControlRoomPalette.TextMuted,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
    };

    private static TextBlock ScaleLabel(Avalonia.Layout.HorizontalAlignment alignment) => new()
    {
        FontSize = 9,
        FontFamily = new FontFamily("Consolas"),
        Foreground = ControlRoomPalette.TextMuted,
        HorizontalAlignment = alignment,
    };

    private sealed class CircularGaugeTrack : Control
    {
        private const double StartDegrees = 135d;
        private const double SweepDegrees = 270d;
        private const int SegmentCount = 90;

        public ControlRoomValueSnapshot? Snapshot { get; set; }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var width = Bounds.Width;
            var height = Bounds.Height;
            if (width <= 20d || height <= 20d)
            {
                return;
            }

            var center = new Point(width / 2d, Math.Min(height * 0.68d, height - 28d));
            var radius = Math.Max(18d, Math.Min(width * 0.39d, height * 0.54d));
            var snapshot = Snapshot;
            var scale = snapshot?.InstrumentScale;

            for (var index = 0; index < SegmentCount; index++)
            {
                var n1 = index / (double)SegmentCount;
                var n2 = (index + 0.72d) / SegmentCount;
                var mid = (n1 + n2) / 2d;
                var brush = scale is null
                    ? ControlRoomPalette.GaugeTrack
                    : ResolveSegmentBrush(scale, scale.Minimum + (mid * (scale.Maximum - scale.Minimum)));
                DrawArcSegment(context, center, radius, n1, n2, new Pen(brush, 8d));
            }

            if (scale is null)
            {
                return;
            }

            if (scale.TargetBand is { } target)
            {
                DrawArcSegment(
                    context,
                    center,
                    radius + 9d,
                    Normalize(scale, target.Minimum),
                    Normalize(scale, target.Maximum),
                    new Pen(ControlRoomPalette.GaugeTarget, 3d));
            }

            if (scale.Setpoint is { } setpoint)
            {
                DrawRadialTick(context, center, radius, Normalize(scale, setpoint), ControlRoomPalette.InformationAccentStrong, 2d, 11d);
            }

            foreach (var limit in scale.ProtectionLimits)
            {
                DrawRadialTick(context, center, radius, Normalize(scale, limit.Threshold), ControlRoomPalette.GaugeProtection, 3d, 14d);
            }

            for (var index = 0; index <= 10; index++)
            {
                DrawRadialTick(context, center, radius, index / 10d, ControlRoomPalette.GaugeTick, 1d, 5d);
            }

            if (snapshot!.NumericValue.HasValue && double.IsFinite(snapshot.NumericValue.Value))
            {
                var actual = snapshot.NumericValue.Value;
                var normalized = Normalize(scale, Math.Clamp(actual, scale.Minimum, scale.Maximum));
                var angle = DegreesToRadians(StartDegrees + (normalized * SweepDegrees));
                var needleEnd = new Point(
                    center.X + (Math.Cos(angle) * radius * 0.72d),
                    center.Y + (Math.Sin(angle) * radius * 0.72d));
                var markerBrush = snapshot.State switch
                {
                    ControlRoomVisualState.Warning => ControlRoomPalette.Accent(ControlRoomVisualState.Warning),
                    ControlRoomVisualState.Trip => ControlRoomPalette.Accent(ControlRoomVisualState.Trip),
                    ControlRoomVisualState.Unavailable => ControlRoomPalette.Accent(ControlRoomVisualState.Unavailable),
                    _ => ControlRoomPalette.InformationAccentStrong,
                };
                context.DrawLine(new Pen(markerBrush, 3d), center, needleEnd);
                context.DrawEllipse(markerBrush, null, center, 5d, 5d);

                if (actual < scale.Minimum || actual > scale.Maximum)
                {
                    var endpoint = PointOnArc(center, radius + 1d, actual < scale.Minimum ? 0d : 1d);
                    context.DrawEllipse(null, new Pen(markerBrush, 3d), endpoint, 7d, 7d);
                }
            }
            else
            {
                context.DrawEllipse(ControlRoomPalette.Accent(ControlRoomVisualState.Unavailable), null, center, 4d, 4d);
            }
        }

        private static IBrush ResolveSegmentBrush(ControlRoomInstrumentScaleSnapshot scale, double value)
        {
            foreach (var limit in scale.ProtectionLimits)
            {
                if ((limit.Direction == ControlRoomLimitDirection.High && value >= limit.Threshold)
                    || (limit.Direction == ControlRoomLimitDirection.Low && value <= limit.Threshold))
                {
                    return ControlRoomPalette.GaugeAlarmBand;
                }
            }

            var band = scale.OperatingBands.FirstOrDefault(item => value >= item.Minimum && value <= item.Maximum);
            return band is null ? ControlRoomPalette.GaugeTrack : ControlRoomPalette.InstrumentBand(band.Kind);
        }

        private static void DrawArcSegment(
            DrawingContext context,
            Point center,
            double radius,
            double normalizedStart,
            double normalizedEnd,
            IPen pen)
        {
            var clampedStart = Math.Clamp(normalizedStart, 0d, 1d);
            var clampedEnd = Math.Clamp(normalizedEnd, 0d, 1d);
            if (clampedEnd <= clampedStart)
            {
                return;
            }

            const int subdivisions = 4;
            var previous = PointOnArc(center, radius, clampedStart);
            for (var index = 1; index <= subdivisions; index++)
            {
                var currentNormalized = clampedStart + ((clampedEnd - clampedStart) * index / subdivisions);
                var current = PointOnArc(center, radius, currentNormalized);
                context.DrawLine(pen, previous, current);
                previous = current;
            }
        }

        private static void DrawRadialTick(
            DrawingContext context,
            Point center,
            double radius,
            double normalized,
            IBrush brush,
            double thickness,
            double length)
        {
            var angle = DegreesToRadians(StartDegrees + (Math.Clamp(normalized, 0d, 1d) * SweepDegrees));
            var inner = new Point(
                center.X + (Math.Cos(angle) * (radius - length)),
                center.Y + (Math.Sin(angle) * (radius - length)));
            var outer = new Point(
                center.X + (Math.Cos(angle) * (radius + 3d)),
                center.Y + (Math.Sin(angle) * (radius + 3d)));
            context.DrawLine(new Pen(brush, thickness), inner, outer);
        }

        private static Point PointOnArc(Point center, double radius, double normalized)
        {
            var angle = DegreesToRadians(StartDegrees + (Math.Clamp(normalized, 0d, 1d) * SweepDegrees));
            return new Point(center.X + (Math.Cos(angle) * radius), center.Y + (Math.Sin(angle) * radius));
        }

        private static double Normalize(ControlRoomInstrumentScaleSnapshot scale, double value)
            => (value - scale.Minimum) / (scale.Maximum - scale.Minimum);

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
    }
}
