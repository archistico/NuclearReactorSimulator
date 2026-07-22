namespace NuclearReactorSimulator.Application.ControlRoom.OperatorComputer;

public sealed record OperatorComputerSessionCheckpointSnapshot(
    string CheckpointId,
    long LogicalStep,
    string SnapshotFingerprint)
{
    public string DisplayText => $"{CheckpointId} · STEP {LogicalStep:D8}";
}
