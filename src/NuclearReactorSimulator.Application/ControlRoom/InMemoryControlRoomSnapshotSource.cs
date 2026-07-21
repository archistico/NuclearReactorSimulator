namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Small presentation boundary useful for composition/tests. Publishing a snapshot never advances simulation state.
/// </summary>
public sealed class InMemoryControlRoomSnapshotSource : IControlRoomSnapshotSource
{
    public InMemoryControlRoomSnapshotSource(ControlRoomSnapshot initialSnapshot)
    {
        Current = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
    }

    public ControlRoomSnapshot Current { get; private set; }

    public event EventHandler<ControlRoomSnapshotChangedEventArgs>? SnapshotChanged;

    public void Publish(ControlRoomSnapshot snapshot)
    {
        Current = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        SnapshotChanged?.Invoke(this, new ControlRoomSnapshotChangedEventArgs(snapshot));
    }
}
