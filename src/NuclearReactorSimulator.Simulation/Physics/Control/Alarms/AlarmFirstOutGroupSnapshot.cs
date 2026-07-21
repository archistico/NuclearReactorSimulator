using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

public sealed class AlarmFirstOutGroupSnapshot
{
    public AlarmFirstOutGroupSnapshot(string groupId, string? firstOutAlarmId, IEnumerable<string> annunciatedAlarmIds)
    {
        GroupId = groupId;
        FirstOutAlarmId = firstOutAlarmId;
        AnnunciatedAlarmIds = new ReadOnlyCollection<string>(annunciatedAlarmIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray());
    }

    public string GroupId { get; }
    public string? FirstOutAlarmId { get; }
    public IReadOnlyList<string> AnnunciatedAlarmIds { get; }
}
