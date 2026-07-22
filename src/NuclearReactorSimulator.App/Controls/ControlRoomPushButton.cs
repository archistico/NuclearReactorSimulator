using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

public sealed class ControlRoomPushButton : Button
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<ControlRoomVisualState> StateProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, ControlRoomVisualState>(nameof(State), ControlRoomVisualState.Normal);

    public static readonly StyledProperty<bool> IsCommandEnabledProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, bool>(nameof(IsCommandEnabled), true);

    public ControlRoomPushButton()
    {
        MinWidth = 150;
        MinHeight = 44;
        Padding = new Thickness(14, 10);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);
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

    public bool IsCommandEnabled
    {
        get => GetValue(IsCommandEnabledProperty);
        set => SetValue(IsCommandEnabledProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LabelProperty || change.Property == StateProperty || change.Property == IsCommandEnabledProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        Content = Label;
        BorderBrush = ControlRoomPalette.Accent(State);
        BorderThickness = new Thickness(2);
        Background = ControlRoomPalette.ControlBackground(State);
        Foreground = ControlRoomPalette.ControlForeground(State);
        IsEnabled = State != ControlRoomVisualState.Unavailable && IsCommandEnabled;
        Opacity = 1d;
        Cursor = new Cursor(IsEnabled ? StandardCursorType.Hand : StandardCursorType.Arrow);
    }
}
