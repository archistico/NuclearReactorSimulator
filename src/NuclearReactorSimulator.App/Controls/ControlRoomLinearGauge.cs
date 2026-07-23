using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

/// <summary>
/// M10.9.2 banded linear instrument. It renders immutable Application HMI semantics and never invents thresholds.
/// Values outside the configured display scale are explicitly marked off-scale rather than silently presented as in-range.
/// </summary>
public sealed class ControlRoomLinearGauge : Border
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomLinearGauge, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<ControlRoomValueSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<ControlRoomLinearGauge, ControlRoomValueSnapshot?>(nameof(Snapshot));

    public static readonly StyledProperty<ControlRoomInstrumentTrendSnapshot?> TrendProperty =
        AvaloniaProperty.Register<ControlRoomLinearGauge, ControlRoomInstrumentTrendSnapshot?>(nameof(Trend));

    private readonly TextBlock _label;
    private readonly TextBlock _provenance;
    private readonly TextBlock _quality;
    private readonly TextBlock _value;
    private readonly TextBlock _unit;
    private readonly TextBlock _state;
    private readonly TextBlock _minimum;
    private readonly TextBlock _maximum;
    private readonly TextBlock _scaleStatus;
    private readonly TextBlock _semantics;
    private readonly TextBlock _trend;
    private readonly LinearGaugeTrack _track;

    public ControlRoomLinearGauge()
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
        _value = new TextBlock
        {
            FontSize = 24,
            FontWeight = FontWeight.SemiBold,
            FontFamily = new FontFamily("Consolas"),
        };
        _unit = new TextBlock
        {
            FontSize = 11,
            Foreground = ControlRoomPalette.TextMuted,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
            Margin = new Thickness(5, 0, 0, 3),
        };
        _state = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };
        _track = new LinearGaugeTrack { Height = 46 };
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
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 6 };
        header.Children.Add(_label);
        Grid.SetColumn(_provenance, 1);
        header.Children.Add(_provenance);
        Grid.SetColumn(_quality, 2);
        header.Children.Add(_quality);

        var valueRow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        var valueStack = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        valueStack.Children.Add(_value);
        valueStack.Children.Add(_unit);
        valueRow.Children.Add(valueStack);
        Grid.SetColumn(_state, 2);
        valueRow.Children.Add(_state);

        var scaleRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*") };
        scaleRow.Children.Add(_minimum);
        Grid.SetColumn(_scaleStatus, 1);
        scaleRow.Children.Add(_scaleStatus);
        Grid.SetColumn(_maximum, 2);
        scaleRow.Children.Add(_maximum);

        Child = new StackPanel
        {
            Spacing = 6,
            Children = { header, valueRow, _track, scaleRow, _semantics, _trend },
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
        var accent = ControlRoomPalette.Accent(state);

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
        _state.Foreground = accent;
        _minimum.Text = scale is null ? "min —" : $"{scale.Minimum:0.###}";
        _maximum.Text = scale is null ? "max —" : $"{scale.Maximum:0.###}";
        _scaleStatus.Text = snapshot?.ScaleStatusText ?? "SCALE —";
        _scaleStatus.Foreground = snapshot?.IsOffScale == true
            ? ControlRoomPalette.Accent(ControlRoomVisualState.Warning)
            : ControlRoomPalette.TextMuted;
        _semantics.Text = snapshot?.ScaleSemanticsText ?? "RANGES —";
        _trend.Text = trend.Direction == ControlRoomInstrumentTrendDirection.Unavailable
            ? "TREND —"
            : $"TREND {trend.ArrowText} {trend.DirectionText} · {trend.RateText}";

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

    private sealed class LinearGaugeTrack : Control
    {
        public ControlRoomValueSnapshot? Snapshot { get; set; }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            var width = Bounds.Width;
            if (width <= 8d)
            {
                return;
            }

            const double left = 5d;
            const double right = 5d;
            const double top = 12d;
            const double height = 16d;
            var trackWidth = Math.Max(1d, width - left - right);
            var track = new Rect(left, top, trackWidth, height);
            context.DrawRectangle(ControlRoomPalette.GaugeTrackDark, new Pen(ControlRoomPalette.Border, 1d), track, 2d, 2d);

            var snapshot = Snapshot;
            var scale = snapshot?.InstrumentScale;
            if (scale is null)
            {
                return;
            }

            foreach (var band in scale.OperatingBands)
            {
                DrawRange(context, scale, track, band.Minimum, band.Maximum, ControlRoomPalette.InstrumentBand(band.Kind), top + 2d, height - 4d);
            }

            foreach (var limit in scale.ProtectionLimits)
            {
                if (limit.Direction == ControlRoomLimitDirection.High)
                {
                    DrawRange(context, scale, track, limit.Threshold, scale.Maximum, ControlRoomPalette.GaugeAlarmBand, top + height - 4d, 3d);
                }
                else
                {
                    DrawRange(context, scale, track, scale.Minimum, limit.Threshold, ControlRoomPalette.GaugeAlarmBand, top + height - 4d, 3d);
                }
            }

            if (scale.TargetBand is { } target)
            {
                DrawRange(context, scale, track, target.Minimum, target.Maximum, ControlRoomPalette.GaugeTarget, top - 4d, 3d);
            }

            if (scale.Setpoint is { } setpoint)
            {
                var x = Map(scale, track, setpoint);
                context.DrawLine(new Pen(ControlRoomPalette.InformationAccentStrong, 2d), new Point(x, top - 7d), new Point(x, top + height + 6d));
            }

            foreach (var limit in scale.ProtectionLimits)
            {
                var x = Map(scale, track, limit.Threshold);
                context.DrawLine(new Pen(ControlRoomPalette.GaugeProtection, 2.5d), new Point(x, top - 7d), new Point(x, top + height + 6d));
            }

            for (var index = 0; index <= 10; index++)
            {
                var x = left + (trackWidth * index / 10d);
                context.DrawLine(new Pen(ControlRoomPalette.GaugeTick, 1d), new Point(x, top + height + 2d), new Point(x, top + height + 6d));
            }

            if (!snapshot!.NumericValue.HasValue || !double.IsFinite(snapshot.NumericValue.Value))
            {
                return;
            }

            var actual = snapshot.NumericValue.Value;
            var clamped = Math.Clamp(actual, scale.Minimum, scale.Maximum);
            var markerX = Map(scale, track, clamped);
            var markerBrush = snapshot.State switch
            {
                ControlRoomVisualState.Warning => ControlRoomPalette.Accent(ControlRoomVisualState.Warning),
                ControlRoomVisualState.Trip => ControlRoomPalette.Accent(ControlRoomVisualState.Trip),
                ControlRoomVisualState.Unavailable => ControlRoomPalette.Accent(ControlRoomVisualState.Unavailable),
                _ => ControlRoomPalette.InformationAccentStrong,
            };
            context.DrawLine(new Pen(markerBrush, 3d), new Point(markerX, top - 2d), new Point(markerX, top + height + 2d));
            context.DrawEllipse(markerBrush, null, new Point(markerX, top - 5d), 3.5d, 3.5d);

            if (actual < scale.Minimum)
            {
                DrawOffScaleArrow(context, markerBrush, left, top + (height / 2d), pointsLeft: true);
            }
            else if (actual > scale.Maximum)
            {
                DrawOffScaleArrow(context, markerBrush, left + trackWidth, top + (height / 2d), pointsLeft: false);
            }
        }

        private static void DrawRange(
            DrawingContext context,
            ControlRoomInstrumentScaleSnapshot scale,
            Rect track,
            double minimum,
            double maximum,
            IBrush brush,
            double y,
            double height)
        {
            var x1 = Map(scale, track, minimum);
            var x2 = Map(scale, track, maximum);
            context.DrawRectangle(brush, null, new Rect(Math.Min(x1, x2), y, Math.Max(1d, Math.Abs(x2 - x1)), height), 1d, 1d);
        }

        private static double Map(ControlRoomInstrumentScaleSnapshot scale, Rect track, double value)
        {
            var normalized = (value - scale.Minimum) / (scale.Maximum - scale.Minimum);
            return track.X + (Math.Clamp(normalized, 0d, 1d) * track.Width);
        }

        private static void DrawOffScaleArrow(DrawingContext context, IBrush brush, double x, double y, bool pointsLeft)
        {
            var direction = pointsLeft ? -1d : 1d;
            var pen = new Pen(brush, 2d);
            context.DrawLine(pen, new Point(x, y), new Point(x - (direction * 8d), y - 6d));
            context.DrawLine(pen, new Point(x, y), new Point(x - (direction * 8d), y + 6d));
        }
    }
}
