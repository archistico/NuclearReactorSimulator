using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

public sealed class ControlRoomToggleSwitch : ToggleButton
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomToggleSwitch, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<ControlRoomVisualState> StateProperty =
        AvaloniaProperty.Register<ControlRoomToggleSwitch, ControlRoomVisualState>(nameof(State), ControlRoomVisualState.Normal);

    private readonly TextBlock _label;
    private readonly TextBlock _position;

    public ControlRoomToggleSwitch()
    {
        MinWidth = 150;
        Padding = new Thickness(14, 10);
        HorizontalContentAlignment = HorizontalAlignment.Stretch;

        _label = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
        };
        _position = new TextBlock
        {
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        Grid.SetColumn(_position, 1);
        Content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Children = { _label, _position },
        };

        UpdateVisuals();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
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
            || change.Property == StateProperty
            || change.Property == IsCheckedProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_label is null || _position is null)
        {
            return;
        }

        var accent = ControlRoomPalette.Accent(State);
        _label.Text = Label;
        _position.Text = State == ControlRoomVisualState.Unavailable
            ? "UNAVAILABLE"
            : IsChecked == true ? "ON" : "OFF";
        _position.Foreground = accent;
        BorderBrush = accent;
        BorderThickness = new Thickness(1);
        IsEnabled = State != ControlRoomVisualState.Unavailable;
    }
}
