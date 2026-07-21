using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

public sealed class ControlRoomIndicatorLamp : Border
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomIndicatorLamp, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<ControlRoomVisualState> StateProperty =
        AvaloniaProperty.Register<ControlRoomIndicatorLamp, ControlRoomVisualState>(nameof(State), ControlRoomVisualState.Normal);

    private readonly Border _lamp;
    private readonly TextBlock _label;
    private readonly TextBlock _stateText;

    public ControlRoomIndicatorLamp()
    {
        Padding = new Thickness(12, 10);
        CornerRadius = new CornerRadius(6);
        Background = ControlRoomPalette.SurfaceInset;
        BorderBrush = ControlRoomPalette.Border;
        BorderThickness = new Thickness(1);

        _lamp = new Border
        {
            Width = 18,
            Height = 18,
            CornerRadius = new CornerRadius(9),
            BorderThickness = new Thickness(2),
        };
        _label = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _stateText = new TextBlock
        {
            FontSize = 11,
            Foreground = ControlRoomPalette.TextMuted,
            VerticalAlignment = VerticalAlignment.Center,
        };

        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                _lamp,
                new StackPanel
                {
                    Spacing = 2,
                    Children = { _label, _stateText },
                },
            },
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

        if (change.Property == LabelProperty || change.Property == StateProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_lamp is null || _label is null || _stateText is null)
        {
            return;
        }

        var accent = ControlRoomPalette.Accent(State);
        _lamp.Background = accent;
        _lamp.BorderBrush = accent;
        _label.Text = Label;
        _stateText.Text = ControlRoomPalette.StateText(State);
    }
}
