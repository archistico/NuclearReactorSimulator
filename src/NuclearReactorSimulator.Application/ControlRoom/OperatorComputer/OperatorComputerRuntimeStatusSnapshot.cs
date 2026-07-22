using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerRuntimeStatusSnapshot(
    long LogicalStep,
    ControlRoomRunState RunState,
    int InvalidMeasuredSignalCount,
    int AnnunciatedAlarmCount,
    int UnacknowledgedAlarmCount,
    bool AnyTripActive);
