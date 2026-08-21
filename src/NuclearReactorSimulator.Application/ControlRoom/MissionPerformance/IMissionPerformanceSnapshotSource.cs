namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>Read-only publication seam for the M10.9.7 Mission/Performance presentation surface.</summary>
public interface IMissionPerformanceSnapshotSource
{
    event EventHandler<MissionPerformanceSnapshotChangedEventArgs>? SnapshotChanged;

    MissionPerformanceSnapshot Current { get; }
}
