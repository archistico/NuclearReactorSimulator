namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// Small presentation-source implementation used by composition/tests that already own a canonical Mission/Performance
/// snapshot. Publishing equal presentation content is suppressed through the explicit comparer rather than record equality.
/// </summary>
public sealed class InMemoryMissionPerformanceSnapshotSource : IMissionPerformanceSnapshotSource
{
    private MissionPerformanceSnapshot _current;

    public InMemoryMissionPerformanceSnapshotSource(MissionPerformanceSnapshot initial)
    {
        _current = initial ?? throw new ArgumentNullException(nameof(initial));
    }

    public event EventHandler<MissionPerformanceSnapshotChangedEventArgs>? SnapshotChanged;

    public MissionPerformanceSnapshot Current => _current;

    public bool Publish(MissionPerformanceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (MissionPerformancePresentationComparer.AreEquivalent(_current, snapshot))
        {
            return false;
        }

        _current = snapshot;
        SnapshotChanged?.Invoke(this, new MissionPerformanceSnapshotChangedEventArgs(snapshot));
        return true;
    }
}
