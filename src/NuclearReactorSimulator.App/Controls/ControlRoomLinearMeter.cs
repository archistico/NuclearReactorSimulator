using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

public sealed class ControlRoomLinearMeter : Border
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomLinearMeter, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<ControlRoomLinearMeter, double>(nameof(Minimum), 0d);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<ControlRoomLinearMeter, double>(nameof(Maximum), 100d);

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<ControlRoomLinearMeter, double>(nameof(Value), 0d);

    public static readonly StyledProperty<string> UnitProperty =
        AvaloniaProperty.Register<ControlRoomLinearMeter, string>(nameof(Unit), string.Empty);

    public static readonly StyledProperty<ControlRoomVisualState> StateProperty =
        AvaloniaProperty.Register<ControlRoomLinearMeter, ControlRoomVisualState>(nameof(State), ControlRoomVisualState.Normal);

    private readonly TextBlock _label;
    private readonly TextBlock _valueText;
    private readonly ProgressBar _bar;
    private readonly TextBlock _stateText;

    public ControlRoomLinearMeter()
    {
        Padding = new Thickness(14, 12);
        CornerRadius = new CornerRadius(6);
        Background = ControlRoomPalette.SurfaceInset;
        BorderBrush = ControlRoomPalette.Border;
        BorderThickness = new Thickness(1);

        _label = new TextBlock
        {
            FontSize = 11,
            Foreground = ControlRoomPalette.TextMuted,
        };
        _valueText = new TextBlock
        {
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
        };
        _bar = new ProgressBar
        {
            Height = 10,
        };
        _stateText = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
        };

        Child = new StackPanel
        {
            Spacing = 7,
            Children = { _label, _valueText, _bar, _stateText },
        };

        UpdateVisuals();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LabelProperty
            || change.Property == MinimumProperty
            || change.Property == MaximumProperty
            || change.Property == ValueProperty
            || change.Property == UnitProperty
            || change.Property == StateProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_label is null || _valueText is null || _bar is null || _stateText is null)
        {
            return;
        }

        var minimum = double.IsFinite(Minimum) ? Minimum : 0d;
        var maximum = double.IsFinite(Maximum) && Maximum > minimum ? Maximum : minimum + 1d;
        var value = double.IsFinite(Value) ? Math.Clamp(Value, minimum, maximum) : minimum;
        var accent = ControlRoomPalette.Accent(State);

        _label.Text = Label;
        _valueText.Text = State == ControlRoomVisualState.Unavailable
            ? "—"
            : FormattableString.Invariant($"{Value:0.0} {Unit}").TrimEnd();
        _bar.Minimum = minimum;
        _bar.Maximum = maximum;
        _bar.Value = State == ControlRoomVisualState.Unavailable ? minimum : value;
        _bar.Foreground = accent;
        _stateText.Text = ControlRoomPalette.StateText(State);
        _stateText.Foreground = accent;
    }
}
