using NuclearReactorSimulator.Simulation.Runtime.Invariants;

namespace NuclearReactorSimulator.Simulation.Runtime;

/// <summary>
/// Generic deterministic fixed-timestep runtime. It is driven explicitly by callers and never reads wall-clock time itself.
/// </summary>
public sealed class SimulationRuntime<TState, TCommand, TStateSnapshot>
    where TState : notnull
    where TCommand : notnull
    where TStateSnapshot : notnull
{
    private readonly object _syncRoot = new();
    private readonly ISimulationKernel<TState, TCommand, TStateSnapshot> _kernel;
    private readonly IReadOnlyList<ISimulationInvariant<TState>> _invariants;
    private readonly SimulationCommandQueue<TCommand> _commandQueue = new();
    private readonly SimulationClock _clock;
    private TState _state;
    private TStateSnapshot _stateSnapshot;
    private SimulationRunState _runState = SimulationRunState.Paused;
    private SimulationSpeed _speed = SimulationSpeed.Normal;
    private SimulationFaultSnapshot? _fault;
    private long _bufferedSimulationTicks;
    private int _scaledQuarterTickRemainder;

    public SimulationRuntime(
        TimeSpan fixedTimeStep,
        TState initialState,
        ISimulationKernel<TState, TCommand, TStateSnapshot> kernel,
        IEnumerable<ISimulationInvariant<TState>>? invariants = null)
    {
        ArgumentNullException.ThrowIfNull(initialState);
        ArgumentNullException.ThrowIfNull(kernel);

        _clock = new SimulationClock(fixedTimeStep);
        _state = initialState;
        _kernel = kernel;
        _invariants = Array.AsReadOnly((invariants ?? Array.Empty<ISimulationInvariant<TState>>()).ToArray());

        _stateSnapshot = _kernel.CreateSnapshot(_state);
        ArgumentNullException.ThrowIfNull(_stateSnapshot);
    }

    public long EnqueueCommand(TCommand command)
    {
        lock (_syncRoot)
        {
            EnsureOperationalUnsafe();
            return _commandQueue.Enqueue(command);
        }
    }

    public void Pause()
    {
        lock (_syncRoot)
        {
            EnsureOperationalUnsafe();
            _runState = SimulationRunState.Paused;
        }
    }

    public void Resume()
    {
        lock (_syncRoot)
        {
            EnsureOperationalUnsafe();
            _runState = SimulationRunState.Running;
        }
    }

    public void SetSpeed(SimulationSpeed speed)
    {
        ArgumentNullException.ThrowIfNull(speed);

        if (!SimulationSpeed.Supported.Contains(speed))
        {
            throw new ArgumentOutOfRangeException(nameof(speed), "The requested simulation speed is not supported.");
        }

        lock (_syncRoot)
        {
            EnsureOperationalUnsafe();
            _speed = speed;
        }
    }

    /// <summary>
    /// Advances the runtime from an externally supplied elapsed duration. The duration source is deliberately outside the engine.
    /// </summary>
    public SimulationAdvanceResult<TStateSnapshot> Advance(TimeSpan elapsedExternalTime)
    {
        if (elapsedExternalTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedExternalTime), "Elapsed time cannot be negative.");
        }

        lock (_syncRoot)
        {
            EnsureOperationalUnsafe();

            if (_runState == SimulationRunState.Paused || elapsedExternalTime == TimeSpan.Zero)
            {
                return new SimulationAdvanceResult<TStateSnapshot>(0, CreateSnapshotUnsafe());
            }

            AccumulateScaledTime(elapsedExternalTime);

            long stepsExecuted = 0;
            while (_bufferedSimulationTicks >= _clock.FixedTimeStep.Ticks)
            {
                ExecuteOneStepUnsafe();
                _bufferedSimulationTicks -= _clock.FixedTimeStep.Ticks;
                stepsExecuted = checked(stepsExecuted + 1);
            }

            return new SimulationAdvanceResult<TStateSnapshot>(stepsExecuted, CreateSnapshotUnsafe());
        }
    }

    /// <summary>
    /// Executes exactly one fixed physical step while paused.
    /// </summary>
    public SimulationSnapshot<TStateSnapshot> StepOnce()
    {
        lock (_syncRoot)
        {
            EnsureOperationalUnsafe();

            if (_runState != SimulationRunState.Paused)
            {
                throw new InvalidOperationException("Single-step execution is only valid while the simulation is paused.");
            }

            ExecuteOneStepUnsafe();
            return CreateSnapshotUnsafe();
        }
    }

    public SimulationSnapshot<TStateSnapshot> GetSnapshot()
    {
        lock (_syncRoot)
        {
            return CreateSnapshotUnsafe();
        }
    }

    private void AccumulateScaledTime(TimeSpan elapsedExternalTime)
    {
        var scaledQuarterTicks = checked(
            (elapsedExternalTime.Ticks * (long)_speed.QuarterUnits) + _scaledQuarterTickRemainder);

        var wholeSimulationTicks = scaledQuarterTicks / SimulationSpeed.ScalingDenominator;
        _scaledQuarterTickRemainder = (int)(scaledQuarterTicks % SimulationSpeed.ScalingDenominator);
        _bufferedSimulationTicks = checked(_bufferedSimulationTicks + wholeSimulationTicks);
    }

    private void ExecuteOneStepUnsafe()
    {
        var context = _clock.CreateNextStepContext();
        var commands = _commandQueue.Drain();

        try
        {
            var nextState = _kernel.Step(_state, commands, context);
            ArgumentNullException.ThrowIfNull(nextState);

            ValidateInvariants(nextState, context);

            var nextStateSnapshot = _kernel.CreateSnapshot(nextState);
            ArgumentNullException.ThrowIfNull(nextStateSnapshot);

            _clock.CommitStep(context);
            _state = nextState;
            _stateSnapshot = nextStateSnapshot;
        }
        catch (Exception exception)
        {
            _commandQueue.RestoreToFront(commands);
            _fault = CreateFaultSnapshot(context, exception);
            _runState = SimulationRunState.Faulted;
            throw new SimulationRuntimeFaultException(_fault, exception);
        }
    }

    private void ValidateInvariants(TState candidateState, SimulationStepContext context)
    {
        foreach (var invariant in _invariants)
        {
            var result = invariant.Evaluate(candidateState, context);
            if (!result.IsSatisfied)
            {
                throw new SimulationInvariantViolationException(
                    invariant.Name,
                    result.FailureReason ?? "The invariant did not provide a failure reason.",
                    context.StepIndex);
            }
        }
    }

    private static SimulationFaultSnapshot CreateFaultSnapshot(
        SimulationStepContext context,
        Exception exception)
    {
        return new SimulationFaultSnapshot(
            context.StepIndex,
            context.StartTime,
            exception.GetType().FullName ?? exception.GetType().Name,
            exception.Message);
    }

    private void EnsureOperationalUnsafe()
    {
        if (_runState == SimulationRunState.Faulted)
        {
            throw new InvalidOperationException(
                "The simulation runtime is faulted. Create a new runtime or restore a future checkpoint before continuing.");
        }
    }

    private SimulationSnapshot<TStateSnapshot> CreateSnapshotUnsafe()
    {
        var runtimeSnapshot = new SimulationRuntimeSnapshot(
            _clock.StepIndex,
            _clock.ElapsedSimulationTime,
            _clock.FixedTimeStep,
            _runState,
            _speed,
            _commandQueue.Count,
            _fault);

        return new SimulationSnapshot<TStateSnapshot>(runtimeSnapshot, _stateSnapshot);
    }
}
