namespace NuclearReactorSimulator.Application.ControlRoom;

public interface IControlRoomSnapshotSource
{
    ControlRoomSnapshot Current { get; }

    event EventHandler<ControlRoomSnapshotChangedEventArgs>? SnapshotChanged;
}
