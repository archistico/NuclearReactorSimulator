using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>
/// M6.7 run/pause/single-step and publication coordinator. Rendering cadence is deliberately outside the deterministic
/// simulation engine: accelerated execution may publish fewer immutable presentation snapshots without changing step count.
/// </summary>
public sealed class ControlRoomRuntimeCoordinator : IControlRoomSnapshotSource, IControlRoomCommandDispatcher, IPlantControlAuthorityDispatcher
{
    private readonly object _gate = new();
    private readonly IControlRoomRuntimeEngine _engine;
    private readonly ControlRoomRuntimeExecutionBudget _executionBudget;
    private ControlRoomRunState _runState;
    private ControlRoomSnapshot _current;

    public ControlRoomRuntimeCoordinator(
        IControlRoomRuntimeEngine engine,
        ControlRoomRunState initialRunState = ControlRoomRunState.Paused,
        ControlRoomRuntimeExecutionBudget? executionBudget = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _executionBudget = executionBudget ?? ControlRoomRuntimeExecutionBudget.DesktopDefault;
        if (initialRunState == ControlRoomRunState.ShellOnly)
        {
            throw new ArgumentException("A connected M6.7 runtime cannot start in ShellOnly state.", nameof(initialRunState));
        }

        _runState = initialRunState;
        _current = engine.CreatePresentationSnapshot(initialRunState);
    }

    public event EventHandler<ControlRoomSnapshotChangedEventArgs>? SnapshotChanged;

    public event EventHandler<PlantControlAuthorityChangedEventArgs>? AuthorityChanged;

    /// <summary>
    /// Raised once for every deterministic simulation step, regardless of presentation publication stride. Observers may
    /// use it for training/evaluation history, but must never mutate physical state or influence deterministic stepping.
    /// </summary>
    public event EventHandler<ControlRoomSnapshotChangedEventArgs>? DeterministicStepCompleted;

    public ControlRoomSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public PlantControlAuthorityPresentationSnapshot CurrentAutomation
    {
        get
        {
            lock (_gate)
            {
                return _engine is IPlantControlAuthorityRuntimeEngine automation
                    ? automation.CreateAutomationSnapshot()
                    : PlantControlAuthorityPresentationSnapshot.Unavailable;
            }
        }
    }

    public ControlRoomRuntimeExecutionBudget ExecutionBudget => _executionBudget;

    public ControlRoomRunState RunState
    {
        get
        {
            lock (_gate)
            {
                return _runState;
            }
        }
    }

    public void RequestAuthority(PlantControlAuthorityMode mode)
    {
        PlantControlAuthorityPresentationSnapshot snapshot;
        lock (_gate)
        {
            var automation = RequireAutomationRuntime();
            automation.RequestPlantControlAuthority(mode);
            snapshot = automation.CreateAutomationSnapshot();
        }

        AuthorityChanged?.Invoke(this, new PlantControlAuthorityChangedEventArgs(snapshot));
    }

    public void RequestSupervisoryObjective(SupervisoryObjectiveRequest objective)
    {
        ArgumentNullException.ThrowIfNull(objective);
        PlantControlAuthorityPresentationSnapshot snapshot;
        lock (_gate)
        {
            var automation = RequireAutomationRuntime();
            automation.RequestSupervisoryObjective(objective);
            snapshot = automation.CreateAutomationSnapshot();
        }

        AuthorityChanged?.Invoke(this, new PlantControlAuthorityChangedEventArgs(snapshot));
    }

    public void Dispatch(ControlRoomCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ControlRoomSnapshot? publish = null;
        ControlRoomSnapshot? completedStep = null;

        lock (_gate)
        {
            switch (command.Kind)
            {
                case ControlRoomCommandKind.Run:
                    _runState = ControlRoomRunState.Running;
                    publish = SetCurrent(_engine.CreatePresentationSnapshot(_runState));
                    break;
                case ControlRoomCommandKind.Pause:
                    _runState = ControlRoomRunState.Paused;
                    publish = SetCurrent(_engine.CreatePresentationSnapshot(_runState));
                    break;
                case ControlRoomCommandKind.SingleStep:
                    _runState = ControlRoomRunState.Paused;
                    completedStep = _engine.Step(_runState);
                    publish = SetCurrent(completedStep);
                    break;
                default:
                    _engine.QueueOperatorCommand(command);
                    break;
            }
        }

        PublishDeterministicStep(completedStep);
        Publish(publish);
    }

    /// <summary>
    /// Executes a deterministic batch only while RUNNING. Publication stride affects presentation traffic only: every
    /// requested logical step is still executed exactly once by the runtime engine.
    /// </summary>
    public ControlRoomRuntimeBatchResult AdvanceRunning(int stepCount, int publicationStride = 1)
    {
        if (stepCount < 0 || stepCount > _executionBudget.MaximumSimulationStepsPerBatch)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepCount),
                stepCount,
                $"Step count must be between 0 and {_executionBudget.MaximumSimulationStepsPerBatch} for one cooperative runtime batch.");
        }
        if (publicationStride <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(publicationStride));
        }

        var publications = new List<ControlRoomSnapshot>();
        var completedSteps = new List<ControlRoomSnapshot>();
        var executed = 0;
        ControlRoomRunState finalRunState;
        long finalStep;

        lock (_gate)
        {
            if (_runState == ControlRoomRunState.Running)
            {
                for (var index = 1; index <= stepCount; index++)
                {
                    var snapshot = _engine.Step(_runState);
                    completedSteps.Add(snapshot);
                    executed++;
                    var shouldPublish = index % publicationStride == 0 || index == stepCount;
                    if (shouldPublish)
                    {
                        publications.Add(SetCurrent(snapshot));
                    }
                }
            }

            finalRunState = _runState;
            finalStep = _engine.LogicalStep;
        }

        foreach (var snapshot in completedSteps)
        {
            PublishDeterministicStep(snapshot);
        }

        foreach (var snapshot in publications)
        {
            Publish(snapshot);
        }

        return new ControlRoomRuntimeBatchResult(stepCount, executed, publications.Count, finalStep, finalRunState);
    }

    private IPlantControlAuthorityRuntimeEngine RequireAutomationRuntime()
        => _engine as IPlantControlAuthorityRuntimeEngine
            ?? throw new InvalidOperationException("The loaded runtime does not expose the M10.5/M10.6 plant-control-authority seam.");

    private ControlRoomSnapshot SetCurrent(ControlRoomSnapshot snapshot)
    {
        _current = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        return snapshot;
    }

    private void PublishDeterministicStep(ControlRoomSnapshot? snapshot)
    {
        if (snapshot is not null)
        {
            DeterministicStepCompleted?.Invoke(this, new ControlRoomSnapshotChangedEventArgs(snapshot));
        }
    }

    private void Publish(ControlRoomSnapshot? snapshot)
    {
        if (snapshot is not null)
        {
            SnapshotChanged?.Invoke(this, new ControlRoomSnapshotChangedEventArgs(snapshot));
        }
    }
}
