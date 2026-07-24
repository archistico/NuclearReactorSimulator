using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

internal static class ControlRoomPalette
{
    private static IBrush NormalAccent { get; } = Brush.Parse("#45D69A");
    private static IBrush WarningAccent { get; } = Brush.Parse("#F2C14E");
    private static IBrush TripAccent { get; } = Brush.Parse("#FF6268");
    private static IBrush UnavailableAccent { get; } = Brush.Parse("#71808B");

    public static IBrush InformationAccent { get; } = Brush.Parse("#62D6E8");
    public static IBrush InformationAccentStrong { get; } = Brush.Parse("#B1F3FA");
    public static IBrush GaugeTrack { get; } = Brush.Parse("#293B46");
    public static IBrush GaugeTrackDark { get; } = Brush.Parse("#14212A");
    public static IBrush GaugeTarget { get; } = Brush.Parse("#54BFD2");
    public static IBrush GaugeProtection { get; } = Brush.Parse("#FF6268");
    public static IBrush GaugeNormalBand { get; } = Brush.Parse("#2F8F68");
    public static IBrush GaugeWarningBand { get; } = Brush.Parse("#A98531");
    public static IBrush GaugeAlarmBand { get; } = Brush.Parse("#B44343");
    public static IBrush GaugeTick { get; } = Brush.Parse("#718895");

    public static IBrush SurfaceInset { get; } = Brush.Parse("#0D1820");
    public static IBrush SurfaceRaised { get; } = Brush.Parse("#14232C");
    public static IBrush Border { get; } = Brush.Parse("#34505D");
    public static IBrush BorderStrong { get; } = Brush.Parse("#4B7180");
    public static IBrush TextMuted { get; } = Brush.Parse("#A9BDC5");
    public static IBrush ActiveControlText { get; } = Brush.Parse("#071116");

    private static IBrush NormalFill { get; } = Brush.Parse("#7DE3B7");
    private static IBrush WarningFill { get; } = Brush.Parse("#F7D77C");
    private static IBrush TripFill { get; } = Brush.Parse("#FF9DA1");

    public static IBrush ControlBackground(ControlRoomVisualState state, bool isActive = false) => state switch
    {
        ControlRoomVisualState.Warning => WarningFill,
        ControlRoomVisualState.Trip => TripFill,
        ControlRoomVisualState.Normal when isActive => NormalFill,
        _ => Brushes.Transparent,
    };

    public static IBrush ControlForeground(ControlRoomVisualState state, bool isActive = false) =>
        state is ControlRoomVisualState.Warning or ControlRoomVisualState.Trip || isActive
            ? ActiveControlText
            : Brushes.White;

    public static IBrush Accent(ControlRoomVisualState state) => state switch
    {
        ControlRoomVisualState.Normal => NormalAccent,
        ControlRoomVisualState.Warning => WarningAccent,
        ControlRoomVisualState.Trip => TripAccent,
        ControlRoomVisualState.Unavailable => UnavailableAccent,
        _ => UnavailableAccent,
    };

    public static IBrush InstrumentBand(ControlRoomInstrumentBandKind kind) => kind switch
    {
        ControlRoomInstrumentBandKind.NormalOperating => GaugeNormalBand,
        ControlRoomInstrumentBandKind.Warning => GaugeWarningBand,
        ControlRoomInstrumentBandKind.Alarm => GaugeAlarmBand,
        _ => GaugeTrack,
    };

    public static string StateText(ControlRoomVisualState state) => state switch
    {
        ControlRoomVisualState.Normal => "NORMAL",
        ControlRoomVisualState.Warning => "WARNING",
        ControlRoomVisualState.Trip => "TRIP",
        ControlRoomVisualState.Unavailable => "UNAVAILABLE",
        _ => "UNAVAILABLE",
    };
}
