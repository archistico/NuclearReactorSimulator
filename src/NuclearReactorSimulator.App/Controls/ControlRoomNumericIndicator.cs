using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;

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

    private readonly TextBlock _label;
    private readonly TextBlock _value;
    private readonly TextBlock _unit;
    private readonly TextBlock _stateText;

    public ControlRoomNumericIndicator()
    {
        Padding = new Thickness(14, 12);
        CornerRadius = new CornerRadius(6);
        Background = ControlRoomPalette.SurfaceInset;
        BorderThickness = new Thickness(1, 1, 1, 3);

        _label = new TextBlock
        {
            FontSize = 11,
            Foreground = ControlRoomPalette.TextMuted,
        };
        _value = new TextBlock
        {
            FontSize = 26,
            FontWeight = FontWeight.SemiBold,
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

        Child = new StackPanel
        {
            Spacing = 4,
            Children = { _label, _value, _unit, _stateText },
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LabelProperty
            || change.Property == ValueTextProperty
            || change.Property == UnitProperty
            || change.Property == StateProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_label is null || _value is null || _unit is null || _stateText is null)
        {
            return;
        }

        var accent = ControlRoomPalette.Accent(State);
        BorderBrush = accent;
        _label.Text = Label;
        _value.Text = State == ControlRoomVisualState.Unavailable ? "—" : ValueText;
        _unit.Text = Unit;
        _stateText.Text = ControlRoomPalette.StateText(State);
        _stateText.Foreground = accent;
    }
}
