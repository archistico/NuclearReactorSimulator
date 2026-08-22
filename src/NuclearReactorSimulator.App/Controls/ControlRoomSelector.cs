using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

public sealed class ControlRoomSelector : Border
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ControlRoomSelector, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> OptionsTextProperty =
        AvaloniaProperty.Register<ControlRoomSelector, string>(nameof(OptionsText), string.Empty);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<ControlRoomSelector, int>(nameof(SelectedIndex), 0);

    public static readonly StyledProperty<ControlRoomVisualState> StateProperty =
        AvaloniaProperty.Register<ControlRoomSelector, ControlRoomVisualState>(nameof(State), ControlRoomVisualState.Normal);

    private readonly TextBlock _label;
    private readonly ComboBox _selector;
    private readonly TextBlock _stateText;
    private IReadOnlyList<string> _options = Array.Empty<string>();
    private bool _updatingSelection;

    public ControlRoomSelector()
    {
        Padding = new Thickness(13, 10);
        CornerRadius = new CornerRadius(4);
        Background = ControlRoomPalette.SurfaceInset;
        BorderBrush = ControlRoomPalette.Border;
        BorderThickness = new Thickness(1, 1, 1, 3);

        _label = new TextBlock
        {
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 0.7,
            Foreground = ControlRoomPalette.TextMuted,
        };
        _selector = new ComboBox
        {
            MinWidth = 150,
            MinHeight = 38,
            FontSize = 12.5,
        };
        _selector.SelectionChanged += (_, _) =>
        {
            if (!_updatingSelection)
            {
                SelectedIndex = _selector.SelectedIndex;
            }
        };
        _stateText = new TextBlock
        {
            FontSize = 9.5,
            FontWeight = FontWeight.SemiBold,
        };

        Child = new StackPanel
        {
            Spacing = 7,
            Children = { _label, _selector, _stateText },
        };

        UpdateVisuals();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string OptionsText
    {
        get => GetValue(OptionsTextProperty);
        set => SetValue(OptionsTextProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
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
            || change.Property == OptionsTextProperty
            || change.Property == SelectedIndexProperty
            || change.Property == StateProperty)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_label is null || _selector is null || _stateText is null)
        {
            return;
        }

        var options = OptionsText
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var optionsChanged = !HasEquivalentOptions(_options, options);
        var selectedIndex = options.Length == 0 ? -1 : Math.Clamp(SelectedIndex, 0, options.Length - 1);
        var accent = ControlRoomPalette.Accent(State);

        _label.Text = Label;
        _updatingSelection = true;
        try
        {
            if (optionsChanged)
            {
                _options = options;
                _selector.ItemsSource = _options;
            }
            if (_selector.SelectedIndex != selectedIndex)
            {
                _selector.SelectedIndex = selectedIndex;
            }
        }
        finally
        {
            _updatingSelection = false;
        }
        _selector.IsEnabled = State != ControlRoomVisualState.Unavailable;
        _stateText.Text = ControlRoomPalette.StateText(State);
        _stateText.Foreground = accent;
        BorderBrush = accent;
    }

    private static bool HasEquivalentOptions(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        for (var index = 0; index < left.Count; index++)
        {
            if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }
}
