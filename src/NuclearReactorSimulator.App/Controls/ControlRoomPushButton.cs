using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

public sealed class ControlRoomPushButton : Button
{
    private readonly DispatcherTimer _pressFeedbackTimer;
    private bool _pressFeedbackActive;

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<ControlRoomVisualState> StateProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, ControlRoomVisualState>(nameof(State), ControlRoomVisualState.Normal);

    public static readonly StyledProperty<bool> IsCommandEnabledProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, bool>(nameof(IsCommandEnabled), true);

    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ControlRoomPushButton, bool>(nameof(IsActive), false);

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

        _pressFeedbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450),
        };
        _pressFeedbackTimer.Tick += OnPressFeedbackTimerTick;
        Click += OnClicked;

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

    /// <summary>
    /// Presentation-only persistent state indicator. Use only when the represented plant/control state is actually active.
    /// Momentary commands should leave this false and rely on the short press feedback pulse plus command-status text.
    /// </summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == LabelProperty
            || change.Property == StateProperty
            || change.Property == IsCommandEnabledProperty
            || change.Property == IsActiveProperty)
        {
            UpdateVisuals();
        }
    }

    private void OnClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _pressFeedbackActive = true;
        _pressFeedbackTimer.Stop();
        _pressFeedbackTimer.Start();
        UpdateVisuals();
    }

    private void OnPressFeedbackTimerTick(object? sender, EventArgs e)
    {
        _pressFeedbackTimer.Stop();
        _pressFeedbackActive = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        var persistentOrPressActive = IsActive || _pressFeedbackActive;
        Content = IsActive && !Label.Contains("ACTIVE", StringComparison.OrdinalIgnoreCase)
            ? $"{Label} — ACTIVE"
            : Label;
        BorderBrush = ControlRoomPalette.Accent(State);
        BorderThickness = new Thickness(2);
        Background = ControlRoomPalette.ControlBackground(State, persistentOrPressActive);
        Foreground = ControlRoomPalette.ControlForeground(State, persistentOrPressActive);
        IsEnabled = State != ControlRoomVisualState.Unavailable && IsCommandEnabled;
        Opacity = 1d;
        Cursor = new Cursor(IsEnabled ? StandardCursorType.Hand : StandardCursorType.Arrow);
    }
}
