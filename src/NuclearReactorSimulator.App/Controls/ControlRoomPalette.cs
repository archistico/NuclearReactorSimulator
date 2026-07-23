using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Hmi;

namespace NuclearReactorSimulator.App.Controls;

internal static class ControlRoomPalette
{
    private static IBrush NormalAccent { get; } = Brush.Parse("#3CCB8C");
    private static IBrush WarningAccent { get; } = Brush.Parse("#E5B94C");
    private static IBrush TripAccent { get; } = Brush.Parse("#EF5B5B");
    private static IBrush UnavailableAccent { get; } = Brush.Parse("#6B7480");

    public static IBrush InformationAccent { get; } = Brush.Parse("#79C9D8");
    public static IBrush InformationAccentStrong { get; } = Brush.Parse("#A6E7F0");
    public static IBrush GaugeTrack { get; } = Brush.Parse("#25333D");
    public static IBrush GaugeTrackDark { get; } = Brush.Parse("#172129");
    public static IBrush GaugeTarget { get; } = Brush.Parse("#4DA8B8");
    public static IBrush GaugeProtection { get; } = Brush.Parse("#EF5B5B");
    public static IBrush GaugeNormalBand { get; } = Brush.Parse("#2F8F68");
    public static IBrush GaugeWarningBand { get; } = Brush.Parse("#A98531");
    public static IBrush GaugeAlarmBand { get; } = Brush.Parse("#B44343");
    public static IBrush GaugeTick { get; } = Brush.Parse("#617480");

    public static IBrush SurfaceInset { get; } = Brush.Parse("#11161D");
    public static IBrush Border { get; } = Brush.Parse("#303846");
    public static IBrush TextMuted { get; } = Brush.Parse("#A7B0BC");
    public static IBrush ActiveControlText { get; } = Brush.Parse("#171C24");

    private static IBrush NormalFill { get; } = Brush.Parse("#8BE0B8");
    private static IBrush WarningFill { get; } = Brush.Parse("#F3D98A");
    private static IBrush TripFill { get; } = Brush.Parse("#FFB4B4");

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
