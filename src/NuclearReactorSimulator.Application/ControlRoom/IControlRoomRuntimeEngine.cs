namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// Deterministic runtime engine boundary used by the M6.7 coordinator. Implementations own simulation stepping and command
/// translation; the coordinator owns run/pause/single-step semantics and snapshot publication cadence only.
/// </summary>
public interface IControlRoomRuntimeEngine
{
    long LogicalStep { get; }

    ControlRoomSnapshot CreatePresentationSnapshot(ControlRoomRunState runState);

    ControlRoomSnapshot Step(ControlRoomRunState runState);

    void QueueOperatorCommand(ControlRoomCommand command);
}
