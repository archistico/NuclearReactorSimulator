namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed class ControlRoomSnapshotChangedEventArgs : EventArgs
{
    public ControlRoomSnapshotChangedEventArgs(ControlRoomSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public ControlRoomSnapshot Snapshot { get; }
}
