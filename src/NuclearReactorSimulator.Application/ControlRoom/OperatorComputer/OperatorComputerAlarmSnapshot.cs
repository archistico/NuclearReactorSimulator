using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerAlarmItemSnapshot(
    string AlarmId,
    string Title,
    string Severity,
    string State,
    bool IsFirstOut,
    bool CanAcknowledge,
    bool CanReset);

public sealed record OperatorComputerAlarmEventSnapshot(
    long Sequence,
    long LogicalStep,
    string AlarmId,
    string AlarmTitle,
    string Kind);

public sealed class OperatorComputerAlarmSnapshot
{
    public OperatorComputerAlarmSnapshot(
        IEnumerable<OperatorComputerAlarmItemSnapshot> alarms,
        IEnumerable<OperatorComputerAlarmEventSnapshot> recentEvents,
        int annunciatedCount,
        int unacknowledgedCount,
        int firstOutCount)
    {
        Alarms = new ReadOnlyCollection<OperatorComputerAlarmItemSnapshot>((alarms ?? throw new ArgumentNullException(nameof(alarms))).ToArray());
        RecentEvents = new ReadOnlyCollection<OperatorComputerAlarmEventSnapshot>((recentEvents ?? throw new ArgumentNullException(nameof(recentEvents))).ToArray());
        if (annunciatedCount < 0 || unacknowledgedCount < 0 || firstOutCount < 0 || unacknowledgedCount > annunciatedCount)
        {
            throw new ArgumentOutOfRangeException(nameof(annunciatedCount));
        }

        AnnunciatedCount = annunciatedCount;
        UnacknowledgedCount = unacknowledgedCount;
        FirstOutCount = firstOutCount;
    }

    public IReadOnlyList<OperatorComputerAlarmItemSnapshot> Alarms { get; }
    public IReadOnlyList<OperatorComputerAlarmEventSnapshot> RecentEvents { get; }
    public int AnnunciatedCount { get; }
    public int UnacknowledgedCount { get; }
    public int FirstOutCount { get; }
}
