namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Presentation-only scalar value used by M6 panels.</summary>
public sealed record ControlRoomValueSnapshot(
    string ValueText,
    string Unit,
    double? NumericValue,
    ControlRoomVisualState State)
{
    public static ControlRoomValueSnapshot Unavailable(string unit = "")
        => new("—", unit, null, ControlRoomVisualState.Unavailable);
}
