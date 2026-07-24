using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

public sealed class ControlRoomNumericIndicator : Border
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomNumericIndicator, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> ValueTextProperty =
        AvaloniaProperty.Register<ControlRoomNumericIndicator, string>(nameof(ValueText), "—");

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<ControlRoomNumericIndicator, string>(nameof(Unit), string.Empty);

    public static readonly StyledProperty<ControlRoomVisualState> StateProperty =
        AvaloniaProperty.Register<ControlRoomNumericIndicator, ControlRoomVisualState>(nameof(State), ControlRoomVisualState.Normal);

    public static readonly StyledProperty<ControlRoomValueSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<ControlRoomNumericIndicator, ControlRoomValueSnapshot?>(nameof(Snapshot));

    public static readonly StyledProperty<ControlRoomInstrumentTrendSnapshot?> TrendProperty =
        AvaloniaProperty.Register<ControlRoomNumericIndicator, ControlRoomInstrumentTrendSnapshot?>(nameof(Trend));

    private readonly TextBlock _label;
    private readonly TextBlock _value;
    private readonly TextBlock _unit;
    private readonly TextBlock _stateText;
    private readonly TextBlock _trendText;
    private readonly ControlRoomAutomaticTrendTracker _automaticTrend = new();

    public ControlRoomNumericIndicator()
    {
        Padding = new Thickness(14, 11);
        CornerRadius = new CornerRadius(4);
        Background = ControlRoomPalette.SurfaceInset;
        BorderThickness = new Thickness(1, 1, 1, 3);

        _label = new TextBlock
        {
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 0.5,
            Foreground = ControlRoomPalette.TextMuted,
        };
        _value = new TextBlock
        {
            FontSize = 25,
            FontWeight = FontWeight.SemiBold,
            FontFamily = ControlRoomTypography.DataFont,
        };
        _unit = new TextBlock
        {
            FontSize = 12,
            Foreground = ControlRoomPalette.TextMuted,
        };
        _stateText = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
        };
        _trendText = new TextBlock
        {
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            FontFamily = ControlRoomTypography.DataFont,
            Foreground = ControlRoomPalette.TextMuted,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
        };

        var statusRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };
        statusRow.Children.Add(_stateText);
        Grid.SetColumn(_trendText, 1);
        statusRow.Children.Add(_trendText);

        Child = new StackPanel
        {
            Spacing = 4,
            Children = { _label, _value, _unit, statusRow },
        };

        UpdateVisuals();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string ValueText
    {
        get => GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public string Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public ControlRoomVisualState State
    {
        get => GetValue(StateProperty);
        set => SetValue(StateProperty, value);
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

        if (change.Property == ControlRoomTrendScope.LogicalStepProperty && !_automaticTrend.HasBaseline)
        {
            _automaticTrend.Observe(GetValue(ControlRoomTrendScope.LogicalStepProperty), Snapshot);
            UpdateVisuals();
            return;
        }

        if (change.Property == LabelProperty
            || change.Property == ValueTextProperty
            || change.Property == UnitProperty
            || change.Property == StateProperty
            || change.Property == SnapshotProperty
            || change.Property == TrendProperty)
        {
            if (change.Property == SnapshotProperty)
            {
                _automaticTrend.Observe(GetValue(ControlRoomTrendScope.LogicalStepProperty), Snapshot);
            }

            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_label is null || _value is null || _unit is null || _stateText is null || _trendText is null)
        {
            return;
        }

        var snapshot = Snapshot;
        var state = snapshot?.State ?? State;
        var trend = Trend ?? _automaticTrend.Current;
        var accent = ControlRoomPalette.Accent(state);
        BorderBrush = accent;
        _label.Text = Label;
        _value.Text = state == ControlRoomVisualState.Unavailable ? "—" : snapshot?.ValueText ?? ValueText;
        _unit.Text = snapshot?.Unit ?? Unit;
        _stateText.Text = ControlRoomPalette.StateText(state);
        _stateText.Foreground = accent;
        _trendText.Text = trend.Direction == ControlRoomInstrumentTrendDirection.Unavailable
            ? "TREND —"
            : $"TREND {trend.ArrowText} {trend.DirectionText}";
        _trendText.Foreground = trend.Direction is ControlRoomInstrumentTrendDirection.RisingRapidly
            or ControlRoomInstrumentTrendDirection.FallingRapidly
            ? ControlRoomPalette.InformationAccentStrong
            : ControlRoomPalette.TextMuted;
    }
}
