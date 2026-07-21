using System.Collections.ObjectModel;
using NuclearReactorSimulator.Application.Scenarios.Faults;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Immutable presentation snapshot of all deterministic M8.1 scenario fault states.</summary>
public sealed class ControlRoomFaultStateSnapshot
{
    public ControlRoomFaultStateSnapshot(IEnumerable<ControlRoomFaultStatusSnapshot>? faults = null)
    {
        var canonical = (faults ?? Array.Empty<ControlRoomFaultStatusSnapshot>())
            .Select(fault => fault ?? throw new ArgumentException("Fault snapshots cannot contain null entries.", nameof(faults)))
            .OrderBy(static fault => fault.FaultId, StringComparer.Ordinal)
            .ToArray();

        if (canonical.Select(static fault => fault.FaultId).Distinct(StringComparer.Ordinal).Count() != canonical.Length)
        {
            throw new ArgumentException("Fault snapshot IDs must be unique.", nameof(faults));
        }

        Faults = new ReadOnlyCollection<ControlRoomFaultStatusSnapshot>(canonical);
    }

    public IReadOnlyList<ControlRoomFaultStatusSnapshot> Faults { get; }

    public int PendingCount => Faults.Count(static fault => fault.Lifecycle == ScenarioFaultLifecycleState.Pending);

    public int ActiveCount => Faults.Count(static fault => fault.Lifecycle == ScenarioFaultLifecycleState.Active);

    public int ClearedCount => Faults.Count(static fault => fault.Lifecycle == ScenarioFaultLifecycleState.Cleared);

    public static ControlRoomFaultStateSnapshot Empty { get; } = new();
}
