namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record ControlRoomAlarmEventPresentationSnapshot(
    long Sequence,
    long LogicalStep,
    string AlarmId,
    string AlarmTitle,
    ControlRoomAlarmEventKind Kind)
{
    public string SequenceText => $"#{Sequence}";
    public string LogicalStepText => $"STEP {LogicalStep}";
    public string KindText => Kind.ToString().ToUpperInvariant();
}
