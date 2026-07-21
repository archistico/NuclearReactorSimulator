namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Application command boundary used by Avalonia. Implementations own runtime coordination; views/view models own no physics.
/// </summary>
public interface IControlRoomCommandDispatcher
{
    void Dispatch(ControlRoomCommand command);
}
