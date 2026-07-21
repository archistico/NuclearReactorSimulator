using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

public sealed class ControlRoomPushButton : Button
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<ControlRoomVisualState> StateProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, ControlRoomVisualState>(nameof(State), ControlRoomVisualState.Normal);

    public ControlRoomPushButton()
    {
        MinWidth = 150;
        Padding = new Thickness(14, 10);
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

        if (change.Property == LabelProperty || change.Property == StateProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        Content = Label;
        BorderBrush = ControlRoomPalette.Accent(State);
        BorderThickness = new Thickness(2);
        IsEnabled = State != ControlRoomVisualState.Unavailable;
    }
}
