namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>Presentation-only M6.6 annunciator row. It mirrors M5.6 operator memory without owning protection.</summary>
public sealed record ControlRoomAlarmPresentationSnapshot(
    string AlarmId,
    string Title,
    ControlRoomAlarmSeverity Severity,
    ControlRoomAlarmAnnunciatorState AnnunciatorState,
    string? FirstOutGroupId,
    bool ConditionActive,
    bool IsLatched,
    bool IsAcknowledged,
    bool IsAnnunciated,
    bool IsFirstOut,
    long? ActivationSequence,
    bool IsLatchedUntilReset)
{
    public string SeverityText => Severity.ToString().ToUpperInvariant();

    public string AnnunciatorText => AnnunciatorState switch
    {
        ControlRoomAlarmAnnunciatorState.ActiveUnacknowledged => "ACTIVE · UNACKNOWLEDGED",
        ControlRoomAlarmAnnunciatorState.ActiveAcknowledged => "ACTIVE · ACKNOWLEDGED",
        ControlRoomAlarmAnnunciatorState.ReturnedUnacknowledged => "RETURNED · UNACKNOWLEDGED",
        ControlRoomAlarmAnnunciatorState.ReturnedAcknowledged => "RETURNED · ACKNOWLEDGED",
        _ => "NORMAL",
    };

    public string FirstOutText => IsFirstOut
        ? $"FIRST OUT · {FirstOutGroupId ?? "UNGROUPED"}"
        : string.Empty;

    public string ActivationSequenceText => ActivationSequence.HasValue ? $"activation #{ActivationSequence.Value}" : string.Empty;

    public bool CanAcknowledge => IsAnnunciated && !IsAcknowledged;

    public bool CanReset => IsLatchedUntilReset && IsLatched && IsAnnunciated && !ConditionActive && IsAcknowledged;

    public ControlRoomVisualState VisualState => !IsAnnunciated
        ? ControlRoomVisualState.Normal
        : Severity switch
        {
            ControlRoomAlarmSeverity.Trip => ControlRoomVisualState.Trip,
            ControlRoomAlarmSeverity.Warning => ControlRoomVisualState.Warning,
            _ => ControlRoomVisualState.Normal,
        };
}
