using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>
/// M10.9.6.1 deterministic challenge lifecycle tracker. It consumes read-only logical-step/action evidence, never wall-clock
/// time, and owns no plant command or control-authority seam.
/// </summary>
public sealed class ScenarioChallengeTracker : IDisposable
{
    private readonly object _gate = new();
    private readonly IChallengeEvidenceSource _evidence;
    private readonly IChallengeConditionEvaluator _evaluator;
    private readonly Dictionary<string, ChallengeConditionObservation> _observations = new(StringComparer.Ordinal);
    private readonly List<ChallengeLifecycleTransition> _transitions = new();
    private ChallengeLifecycleState _state = ChallengeLifecycleState.NotStarted;
    private long _logicalStep;
    private long _nextTransitionSequence = 1;
    private long? _activatedLogicalStep;
    private long? _terminalLogicalStep;
    private bool _disposed;

    private ScenarioChallengeTracker(
        ScenarioDefinition scenario,
        IChallengeEvidenceSource evidence,
        ChallengeDefinition definition,
        IChallengeConditionEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        _evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

        if (!string.Equals(scenario.ScenarioId, definition.ScenarioId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Challenge scenario ID must match the loaded scenario.", nameof(definition));
        }
        if (!scenario.Objectives.Any(objective => string.Equals(objective.ObjectiveId, definition.ObjectiveId, StringComparison.Ordinal)))
        {
            throw new ArgumentException($"Challenge objective '{definition.ObjectiveId}' is not declared by scenario '{scenario.ScenarioId}'.", nameof(definition));
        }

        _logicalStep = evidence.Current.LogicalStep;
        _evidence.DeterministicStepCompleted += OnDeterministicStepCompleted;
        _evidence.OperatorActionAccepted += OnOperatorActionAccepted;
        Evaluate(evidence.Current);
    }

    public event EventHandler<ChallengeLifecycleChangedEventArgs>? LifecycleChanged;

    public ChallengeDefinition Definition { get; }

    public ChallengeLifecycleSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return BuildSnapshot();
            }
        }
    }

    public static ScenarioChallengeTracker Attach(
        ScenarioSession session,
        ChallengeDefinition definition,
        IChallengeConditionEvaluator evaluator)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new ScenarioChallengeTracker(
            session.Scenario,
            new ScenarioChallengeEvidenceSource(session),
            definition,
            evaluator);
    }

    public void Cancel(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ChallengeLifecycleSnapshot snapshot;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (IsTerminal(_state))
            {
                throw new InvalidOperationException($"Challenge '{Definition.ExactId}' is already terminal in state {_state}.");
            }
            TransitionTo(ChallengeLifecycleState.Cancelled, _evidence.Current.LogicalStep, reason.Trim());
            snapshot = BuildSnapshot();
        }
        LifecycleChanged?.Invoke(this, new ChallengeLifecycleChangedEventArgs(snapshot));
    }

    /// <summary>
    /// Explicit session-lifecycle reset. Presentation navigation never calls this API. The reset is immediately reconciled
    /// against the current logical evidence so a challenge may become READY/ACTIVE again deterministically.
    /// </summary>
    public void Reset(string reason = "Explicit challenge reset.")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ChallengeLifecycleSnapshot snapshot;
        lock (_gate)
        {
            ThrowIfDisposed();
            var currentStep = _evidence.Current.LogicalStep;
            TransitionTo(ChallengeLifecycleState.NotStarted, currentStep, reason.Trim());
            _activatedLogicalStep = null;
            _terminalLogicalStep = null;
            _observations.Clear();
            EvaluateLocked(_evidence.Current);
            snapshot = BuildSnapshot();
        }
        LifecycleChanged?.Invoke(this, new ChallengeLifecycleChangedEventArgs(snapshot));
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _evidence.DeterministicStepCompleted -= OnDeterministicStepCompleted;
            _evidence.OperatorActionAccepted -= OnOperatorActionAccepted;
        }
    }

    private void OnDeterministicStepCompleted(object? sender, ControlRoomSnapshotChangedEventArgs e) => Evaluate(e.Snapshot);

    private void OnOperatorActionAccepted(object? sender, ScenarioOperatorActionAcceptedEventArgs e) => Evaluate(_evidence.Current);

    private void Evaluate(ControlRoomSnapshot snapshot)
    {
        ChallengeLifecycleSnapshot lifecycleSnapshot;
        var changed = false;
        lock (_gate)
        {
            ThrowIfDisposed();
            var previousTransitionCount = _transitions.Count;
            var previousObservationFingerprint = ObservationFingerprint();
            EvaluateLocked(snapshot);
            changed = previousTransitionCount != _transitions.Count
                || !string.Equals(previousObservationFingerprint, ObservationFingerprint(), StringComparison.Ordinal);
            lifecycleSnapshot = BuildSnapshot();
        }
        if (changed)
        {
            LifecycleChanged?.Invoke(this, new ChallengeLifecycleChangedEventArgs(lifecycleSnapshot));
        }
    }

    private void EvaluateLocked(ControlRoomSnapshot snapshot)
    {
        if (IsTerminal(_state))
        {
            return;
        }
        if (snapshot.LogicalStep < _logicalStep)
        {
            throw new InvalidOperationException("Challenge evidence logical step cannot move backwards.");
        }
        _logicalStep = snapshot.LogicalStep;

        if (_state == ChallengeLifecycleState.NotStarted
            && snapshot.LogicalStep >= Definition.LogicalTime.ReadyAtLogicalStep)
        {
            TransitionTo(ChallengeLifecycleState.Ready, snapshot.LogicalStep, "Challenge reached its authored logical-time readiness boundary.");
        }

        if (_state == ChallengeLifecycleState.Ready)
        {
            var activation = Observe(Definition.ActivationCondition, snapshot);
            if (activation.IsSatisfied)
            {
                _activatedLogicalStep = snapshot.LogicalStep;
                TransitionTo(ChallengeLifecycleState.Active, snapshot.LogicalStep, $"Activation condition '{activation.ConditionId}' satisfied: {activation.Evidence}");
            }
        }

        if (_state != ChallengeLifecycleState.Active)
        {
            return;
        }

        var allRequiredObservationsSatisfied = true;
        foreach (var required in Definition.RequiredObservations)
        {
            allRequiredObservationsSatisfied &= Observe(required, snapshot).IsSatisfied;
        }

        foreach (var failure in Definition.FailureConditions)
        {
            var observation = Observe(failure, snapshot);
            if (observation.IsSatisfied)
            {
                _terminalLogicalStep = snapshot.LogicalStep;
                TransitionTo(ChallengeLifecycleState.Failed, snapshot.LogicalStep, $"Failure condition '{observation.ConditionId}' satisfied: {observation.Evidence}");
                return;
            }
        }

        var deadline = HardFailureDeadlineLogicalStep();
        if (deadline.HasValue && snapshot.LogicalStep > deadline.Value)
        {
            _terminalLogicalStep = snapshot.LogicalStep;
            TransitionTo(ChallengeLifecycleState.Failed, snapshot.LogicalStep, $"Authored hard logical-step deadline {deadline.Value} elapsed.");
            return;
        }

        var allCompletionSatisfied = true;
        ChallengeConditionObservation? lastCompletion = null;
        foreach (var completion in Definition.CompletionConditions)
        {
            lastCompletion = Observe(completion, snapshot);
            allCompletionSatisfied &= lastCompletion.IsSatisfied;
        }
        if (allRequiredObservationsSatisfied && allCompletionSatisfied)
        {
            _terminalLogicalStep = snapshot.LogicalStep;
            TransitionTo(
                ChallengeLifecycleState.Completed,
                snapshot.LogicalStep,
                $"All authored completion conditions satisfied; latest evidence: {lastCompletion!.Evidence}");
        }
    }

    private ChallengeConditionObservation Observe(ChallengeConditionDefinition condition, ControlRoomSnapshot snapshot)
    {
        var observation = _evaluator.Evaluate(condition, snapshot, _evidence.AcceptedOperatorActions)
            ?? throw new InvalidOperationException($"Challenge evaluator returned null for condition '{condition.ConditionId}'.");
        if (!string.Equals(observation.ConditionId, condition.ConditionId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Challenge evaluator returned condition '{observation.ConditionId}' for requested '{condition.ConditionId}'.");
        }
        if (observation.LogicalStep != snapshot.LogicalStep)
        {
            throw new InvalidOperationException($"Challenge evaluator evidence for '{condition.ConditionId}' must use current logical step {snapshot.LogicalStep}.");
        }
        _observations[condition.ConditionId] = observation;
        return observation;
    }

    private void TransitionTo(ChallengeLifecycleState next, long logicalStep, string reason)
    {
        if (!Enum.IsDefined(next))
        {
            throw new ArgumentOutOfRangeException(nameof(next));
        }
        if (_state == next)
        {
            return;
        }
        _transitions.Add(new ChallengeLifecycleTransition(_nextTransitionSequence++, _state, next, logicalStep, reason));
        _state = next;
    }

    private ChallengeLifecycleSnapshot BuildSnapshot()
    {
        var observations = _observations.Values
            .OrderBy(static item => item.ConditionId, StringComparer.Ordinal)
            .ToArray();
        var transitions = _transitions.ToArray();
        return new ChallengeLifecycleSnapshot(
            Definition.ExactId,
            _state,
            _logicalStep,
            _activatedLogicalStep,
            _terminalLogicalStep,
            TargetWindowStartLogicalStep(),
            TargetWindowEndLogicalStep(),
            HardFailureDeadlineLogicalStep(),
            Array.AsReadOnly(observations),
            Array.AsReadOnly(transitions));
    }

    private long? TargetWindowStartLogicalStep()
        => _activatedLogicalStep.HasValue && Definition.LogicalTime.TargetWindowStartOffsetSteps.HasValue
            ? checked(_activatedLogicalStep.Value + Definition.LogicalTime.TargetWindowStartOffsetSteps.Value)
            : null;

    private long? TargetWindowEndLogicalStep()
        => _activatedLogicalStep.HasValue && Definition.LogicalTime.TargetWindowEndOffsetSteps.HasValue
            ? checked(_activatedLogicalStep.Value + Definition.LogicalTime.TargetWindowEndOffsetSteps.Value)
            : null;

    private long? HardFailureDeadlineLogicalStep()
        => _activatedLogicalStep.HasValue && Definition.LogicalTime.HardFailureDeadlineOffsetSteps.HasValue
            ? checked(_activatedLogicalStep.Value + Definition.LogicalTime.HardFailureDeadlineOffsetSteps.Value)
            : null;

    private string ObservationFingerprint()
        => string.Join(
            "|",
            _observations.Values
                .OrderBy(static item => item.ConditionId, StringComparer.Ordinal)
                .Select(static item => $"{item.ConditionId}:{item.IsSatisfied}:{item.LogicalStep}:{item.Evidence}"));

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ScenarioChallengeTracker));
        }
    }

    private static bool IsTerminal(ChallengeLifecycleState state)
        => state is ChallengeLifecycleState.Completed or ChallengeLifecycleState.Failed or ChallengeLifecycleState.Cancelled;
}
