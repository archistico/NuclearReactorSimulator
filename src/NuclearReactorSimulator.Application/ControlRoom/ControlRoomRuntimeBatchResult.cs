namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record ControlRoomRuntimeBatchResult(
    int RequestedStepCount,
    int ExecutedStepCount,
    int PublishedSnapshotCount,
    long FinalLogicalStep,
    ControlRoomRunState FinalRunState);
