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

    private readonly Func<bool> _isRunning;
    private readonly Func<ControlRoomRuntimeBatchResult> _advance;
    private readonly Action _pause;
    private readonly Action<string> _reportFailure;

    public DesktopControlRoomRuntimePump(
        ControlRoomRuntimeCoordinator coordinator,
        Action<string> reportFailure)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        _isRunning = () => coordinator.RunState == ControlRoomRunState.Running;
        _advance = () => coordinator.AdvanceRunning(SimulationStepsPerTick, publicationStride: SimulationStepsPerTick);
        _pause = () => coordinator.Dispatch(new ControlRoomCommand(ControlRoomCommandKind.Pause));
        _reportFailure = reportFailure ?? throw new ArgumentNullException(nameof(reportFailure));
    }

    internal DesktopControlRoomRuntimePump(
        Func<bool> isRunning,
        Func<ControlRoomRuntimeBatchResult> advance,
        Action pause,
        Action<string> reportFailure)
    {
        _isRunning = isRunning ?? throw new ArgumentNullException(nameof(isRunning));
        _advance = advance ?? throw new ArgumentNullException(nameof(advance));
        _pause = pause ?? throw new ArgumentNullException(nameof(pause));
        _reportFailure = reportFailure ?? throw new ArgumentNullException(nameof(reportFailure));
    }

    public ControlRoomRuntimeBatchResult? Tick()
    {
        if (!_isRunning())
        {
            return null;
        }

        try
        {
            return _advance();
        }
        catch (Exception exception) when (DesktopHostFailurePolicy.IsExpectedDeterministicStepFailure(exception))
        {
            // Expected fail-closed numerical/step failures are host-visible and stop repeated RUN ticks. Unknown/programming
            // failures are intentionally not swallowed by this boundary.
            _pause();
            _reportFailure(exception.Message);
            return null;
        }
    }
}
