using System.Collections.ObjectModel;

namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed class ControlRoomFirstOutGroupPresentationSnapshot
{
    public ControlRoomFirstOutGroupPresentationSnapshot(
        string groupId,
        string? firstOutAlarmId,
        IEnumerable<string> annunciatedAlarmIds)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            throw new ArgumentException("First-out group id cannot be empty or whitespace.", nameof(groupId));
        }

        GroupId = groupId.Trim();
        FirstOutAlarmId = string.IsNullOrWhiteSpace(firstOutAlarmId) ? null : firstOutAlarmId.Trim();
        AnnunciatedAlarmIds = new ReadOnlyCollection<string>(
            (annunciatedAlarmIds ?? throw new ArgumentNullException(nameof(annunciatedAlarmIds))).ToArray());
    }

    public string GroupId { get; }
    public string? FirstOutAlarmId { get; }
    public IReadOnlyList<string> AnnunciatedAlarmIds { get; }
    public string FirstOutText => FirstOutAlarmId is null ? "NO FIRST-OUT" : FirstOutAlarmId;
    public string AnnunciatedText => AnnunciatedAlarmIds.Count == 0
        ? "No annunciated alarms"
        : string.Join(" · ", AnnunciatedAlarmIds);
}
