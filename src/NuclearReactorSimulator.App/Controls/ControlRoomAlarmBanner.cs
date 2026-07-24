using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

/// <summary>
/// Persistent alarm-zone annunciator. The banner mirrors published alarm memory only;
/// it never owns alarm, acknowledgement, reset or protection state.
/// </summary>
public sealed class ControlRoomAlarmBanner : Border
{
    public static readonly StyledProperty<AlarmEventsPanelSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<ControlRoomAlarmBanner, AlarmEventsPanelSnapshot?>(nameof(Snapshot));

    public static readonly StyledProperty<ICommand?> OpenAlarmsCommandProperty =
        AvaloniaProperty.Register<ControlRoomAlarmBanner, ICommand?>(nameof(OpenAlarmsCommand));

    private static readonly IBrush QuietBackground = Brush.Parse("#0A141B");
    private static readonly IBrush WarningZoneBackground = Brush.Parse("#261F0D");
    private static readonly IBrush TripZoneBackground = Brush.Parse("#2B1115");
    private static readonly IBrush QuietTileBackground = Brush.Parse("#0D1A22");
    private static readonly IBrush ReturnedTileBackground = Brush.Parse("#192129");
    private static readonly IBrush WarningTileBackground = Brush.Parse("#6B5315");
    private static readonly IBrush WarningTileFlashBackground = Brush.Parse("#B78A1D");
    private static readonly IBrush TripTileBackground = Brush.Parse("#78282E");
    private static readonly IBrush TripTileFlashBackground = Brush.Parse("#B83B43");

    private readonly DispatcherTimer _flashTimer;
    private readonly TextBlock _zoneState;
    private readonly TextBlock _countText;
    private readonly StackPanel _tiles;
    private bool _flashPhase;
    private string _renderedTileSignature = string.Empty;

    public ControlRoomAlarmBanner()
    {
        Padding = new Thickness(12, 8);
        BorderThickness = new Thickness(2, 2, 2, 3);
        BorderBrush = ControlRoomPalette.Border;
        Background = QuietBackground;

        _zoneState = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = ControlRoomPalette.InformationAccentStrong,
            LetterSpacing = 0.7,
        };
        _countText = new TextBlock
        {
            FontSize = 11,
            Foreground = ControlRoomPalette.TextMuted,
        };
        _tiles = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 7,
        };

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _tiles,
        };

        var layout = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("148,*"),
            ColumnSpacing = 12,
        };
        layout.Children.Add(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 3,
            Children =
            {
                new TextBlock
                {
                    Text = "ALARM ZONE",
                    FontSize = 10,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = ControlRoomPalette.TextMuted,
                    LetterSpacing = 1.2,
                },
                _zoneState,
                _countText,
            },
        });
        Grid.SetColumn(scroll, 1);
        layout.Children.Add(scroll);
        Child = layout;

        _flashTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(550),
        };
        _flashTimer.Tick += (_, _) =>
        {
            _flashPhase = !_flashPhase;
            UpdateVisuals();
        };
        _flashTimer.Start();
        UpdateVisuals();
    }

    public AlarmEventsPanelSnapshot? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public ICommand? OpenAlarmsCommand
    {
        get => GetValue(OpenAlarmsCommandProperty);
        set => SetValue(OpenAlarmsCommandProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SnapshotProperty || change.Property == OpenAlarmsCommandProperty)
        {
            if (change.Property == OpenAlarmsCommandProperty)
            {
                _renderedTileSignature = string.Empty;
            }
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (_tiles is null || _zoneState is null || _countText is null)
        {
            return;
        }

        var snapshot = Snapshot;
        var alarms = snapshot?.Alarms ?? Array.Empty<ControlRoomAlarmPresentationSnapshot>();
        var active = alarms.Where(static alarm => alarm.ConditionActive).ToArray();
        var hasActive = active.Length != 0;
        var hasTrip = active.Any(static alarm => alarm.Severity == ControlRoomAlarmSeverity.Trip);

        Background = !hasActive
            ? QuietBackground
            : hasTrip
                ? TripZoneBackground
                : WarningZoneBackground;
        BorderBrush = !hasActive
            ? ControlRoomPalette.Border
            : ControlRoomPalette.Accent(hasTrip ? ControlRoomVisualState.Trip : ControlRoomVisualState.Warning);
        _zoneState.Text = hasActive
            ? (_flashPhase ? "● ACTIVE" : "◉ ACTIVE")
            : "● CLEAR";
        _zoneState.Foreground = hasActive
            ? ControlRoomPalette.Accent(hasTrip ? ControlRoomVisualState.Trip : ControlRoomVisualState.Warning)
            : ControlRoomPalette.Accent(ControlRoomVisualState.Normal);
        _countText.Text = snapshot is null
            ? "NO DATA"
            : $"{snapshot.AnnunciatedCount} annunciated · {snapshot.UnacknowledgedCount} unack";

        var tileSignature = $"{alarms.Count}|{(hasActive && _flashPhase)}|" + string.Join(
            '\u001f',
            alarms.Select(alarm =>
                $"{alarm.AlarmId}|{alarm.Title}|{alarm.Severity}|{alarm.AnnunciatorState}|{alarm.ConditionActive}|{alarm.IsAnnunciated}|{alarm.IsFirstOut}"));
        if (string.Equals(_renderedTileSignature, tileSignature, StringComparison.Ordinal))
        {
            return;
        }
        _renderedTileSignature = tileSignature;

        _tiles.Children.Clear();
        if (alarms.Count == 0)
        {
            _tiles.Children.Add(new TextBlock
            {
                Text = "No configured alarm channels in the current runtime snapshot",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = ControlRoomPalette.TextMuted,
                FontSize = 12,
            });
            return;
        }

        foreach (var alarm in alarms)
        {
            _tiles.Children.Add(BuildAlarmTile(alarm));
        }
    }

    private Button BuildAlarmTile(ControlRoomAlarmPresentationSnapshot alarm)
    {
        var isTrip = alarm.Severity == ControlRoomAlarmSeverity.Trip;
        var accent = ControlRoomPalette.Accent(alarm.VisualState);
        var activeBackground = isTrip
            ? (_flashPhase ? TripTileFlashBackground : TripTileBackground)
            : (_flashPhase ? WarningTileFlashBackground : WarningTileBackground);
        var background = alarm.ConditionActive
            ? activeBackground
            : alarm.IsAnnunciated
                ? ReturnedTileBackground
                : QuietTileBackground;

        var title = new TextBlock
        {
            Text = alarm.Title.ToUpperInvariant(),
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = alarm.ConditionActive ? Brushes.White : ControlRoomPalette.TextMuted,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        };
        var status = new TextBlock
        {
            Text = alarm.AnnunciatorText,
            FontSize = 9.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = alarm.IsAnnunciated ? accent : Brush.Parse("#718895"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        };
        var firstOut = new TextBlock
        {
            Text = alarm.IsFirstOut ? "◆ FIRST OUT" : alarm.AlarmId,
            FontSize = 9,
            Foreground = alarm.IsFirstOut ? Brushes.White : Brush.Parse("#718895"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
        };

        var button = new Button
        {
            Width = 178,
            Height = 56,
            Padding = new Thickness(10, 6),
            Background = background,
            BorderBrush = alarm.IsAnnunciated ? accent : ControlRoomPalette.Border,
            BorderThickness = new Thickness(alarm.IsFirstOut ? 2 : 1),
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            Command = OpenAlarmsCommand,
            Content = new StackPanel
            {
                Spacing = 1,
                Children = { title, status, firstOut },
            },
        };
        ToolTip.SetTip(button, $"{alarm.Title}\n{alarm.AnnunciatorText}\n{alarm.FirstOutText}");
        return button;
    }
}
