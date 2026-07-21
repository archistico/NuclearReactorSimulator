namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record GeneratorPresentationSnapshot(
    string GeneratorId,
    string RotorId,
    string BreakerId,
    ControlRoomValueSnapshot Frequency,
    ControlRoomValueSnapshot ElectricalOutput,
    ControlRoomValueSnapshot TerminalVoltage,
    ControlRoomValueSnapshot GridVoltage,
    ControlRoomValueSnapshot PhaseDifference,
    ControlRoomValueSnapshot MechanicalInputPower,
    ControlRoomValueSnapshot ConversionLossPower,
    bool SynchronizationConditionsSatisfied,
    bool BreakerClosed,
    bool CloseCommandAccepted,
    bool CloseCommandRejected)
{
    public ControlRoomVisualState SynchronizationState => SynchronizationConditionsSatisfied
        ? ControlRoomVisualState.Normal
        : ControlRoomVisualState.Warning;

    public ControlRoomVisualState BreakerState => CloseCommandRejected
        ? ControlRoomVisualState.Warning
        : ControlRoomVisualState.Normal;

    public string SynchronizationText => SynchronizationConditionsSatisfied ? "SYNCHRONIZATION WINDOW SATISFIED" : "OUTSIDE SYNCHRONIZATION WINDOW";

    public string BreakerText => BreakerClosed ? "BREAKER CLOSED" : "BREAKER OPEN";
}
