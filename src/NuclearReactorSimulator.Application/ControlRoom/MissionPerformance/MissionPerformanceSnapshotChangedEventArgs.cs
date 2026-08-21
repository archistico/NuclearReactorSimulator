namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

public sealed class MissionPerformanceSnapshotChangedEventArgs : EventArgs
{
    public MissionPerformanceSnapshotChangedEventArgs(MissionPerformanceSnapshot snapshot)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public MissionPerformanceSnapshot Snapshot { get; }
}
