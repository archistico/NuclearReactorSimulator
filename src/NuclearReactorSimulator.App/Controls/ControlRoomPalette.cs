using Avalonia.Media;
using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Controls;

internal static class ControlRoomPalette
{
    private static IBrush NormalAccent { get; } = Brush.Parse("#3CCB8C");

    private static IBrush WarningAccent { get; } = Brush.Parse("#E5B94C");

    private static IBrush TripAccent { get; } = Brush.Parse("#EF5B5B");

    private static IBrush UnavailableAccent { get; } = Brush.Parse("#6B7480");

    public static IBrush SurfaceInset { get; } = Brush.Parse("#11161D");

    public static IBrush Border { get; } = Brush.Parse("#303846");

    public static IBrush TextMuted { get; } = Brush.Parse("#A7B0BC");

    public static IBrush ActiveControlText { get; } = Brush.Parse("#171C24");

    private static IBrush WarningFill { get; } = Brush.Parse("#F3D98A");

    private static IBrush TripFill { get; } = Brush.Parse("#FFB4B4");

    public static IBrush ControlBackground(ControlRoomVisualState state) => state switch
    {
        ControlRoomVisualState.Warning => WarningFill,
        ControlRoomVisualState.Trip => TripFill,
        _ => Brushes.Transparent,
    };

    public static IBrush ControlForeground(ControlRoomVisualState state) => state is ControlRoomVisualState.Warning or ControlRoomVisualState.Trip
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

    public static string StateText(ControlRoomVisualState state) => state switch
    {
        ControlRoomVisualState.Normal => "NORMAL",
        ControlRoomVisualState.Warning => "WARNING",
        ControlRoomVisualState.Trip => "TRIP",
        ControlRoomVisualState.Unavailable => "UNAVAILABLE",
        _ => "UNAVAILABLE",
    };
}
