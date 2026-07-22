using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.App.Runtime;

/// <summary>
/// App-layer cooperative RUN host. Wall-clock cadence controls only when deterministic fixed-step batches are requested;
/// it never changes the simulation timestep or physical ownership. One UI tick advances a bounded real-time-equivalent
/// batch and publishes only the final snapshot to avoid unnecessary presentation churn.
/// </summary>
public sealed class DesktopControlRoomRuntimePump
{
    public const int SimulationStepsPerTick = 5;

    private readonly ControlRoomRuntimeCoordinator _coordinator;
    private readonly Action<string> _reportFailure;

    public DesktopControlRoomRuntimePump(
        ControlRoomRuntimeCoordinator coordinator,
        Action<string> reportFailure)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _reportFailure = reportFailure ?? throw new ArgumentNullException(nameof(reportFailure));
    }

    public ControlRoomRuntimeBatchResult? Tick()
    {
        if (_coordinator.RunState != ControlRoomRunState.Running)
        {
            return null;
        }

        try
        {
            return _coordinator.AdvanceRunning(
                SimulationStepsPerTick,
                publicationStride: SimulationStepsPerTick);
        }
        catch (InvalidOperationException exception)
        {
            // A deterministic step failure must be visible and must not create an exception storm on every UI timer tick.
            // Pause only the host execution mode; never mutate physical/protection state to hide the failure.
            _coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
            _reportFailure(exception.Message);
            return null;
        }
    }
}
