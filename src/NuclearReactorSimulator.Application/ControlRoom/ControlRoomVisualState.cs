namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Presentation-only semantic state shared by reusable M6 control-room components.
/// </summary>
public enum ControlRoomVisualState
{
    Normal = 0,
    Warning = 1,
    Trip = 2,
    Unavailable = 3,
}
