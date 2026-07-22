using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>
/// M8.1 deterministic fault-scheduling decorator. Transitions are evaluated only at committed logical-step boundaries.
/// Activation/deactivation effects are delegated to registered runtime-bound applicators before the corresponding physical
/// step. The decorator never advances time, mutates presentation state to force outcomes or evaluates wall clock.
/// </summary>
public sealed class ScenarioFaultRuntimeEngine : IControlRoomRuntimeEngine, IPlantControlAuthorityRuntimeEngine
{
    private sealed class RuntimeFaultState
    {
        public RuntimeFaultState(ScenarioFaultDefinition definition)
        {
            Definition = definition;
        }

        public ScenarioFaultDefinition Definition { get; }

        public ScenarioFaultLifecycleState Lifecycle { get; set; } = ScenarioFaultLifecycleState.Pending;

        public long? ActivatedLogicalStep { get; set; }

        public long? ClearedLogicalStep { get; set; }

        public long LastTransitionSequence { get; set; }
    }

    private readonly IControlRoomRuntimeEngine _inner;
    private readonly IReadOnlyDictionary<string, IScenarioFaultApplicator> _applicators;
    private readonly ScenarioFaultConditionRegistry _conditions;
    private readonly IReadOnlyList<RuntimeFaultState> _faults;
    private long _transitionSequence;
    private bool _initialized;

    public ScenarioFaultRuntimeEngine(
        IControlRoomRuntimeEngine inner,
        IEnumerable<ScenarioFaultDefinition> faults,
        IReadOnlyDictionary<string, IScenarioFaultApplicator> applicators,
        ScenarioFaultConditionRegistry? conditions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        ArgumentNullException.ThrowIfNull(faults);
        _applicators = applicators ?? throw new ArgumentNullException(nameof(applicators));
        _conditions = conditions ?? ScenarioFaultConditionRegistry.Empty;

        var definitions = faults
            .Select(fault => fault ?? throw new ArgumentException("Scenario faults cannot contain null entries.", nameof(faults)))
            .OrderBy(static fault => fault.FaultId, StringComparer.Ordinal)
            .ToArray();
        if (definitions.Select(static fault => fault.FaultId).Distinct(StringComparer.Ordinal).Count() != definitions.Length)
        {
            throw new ArgumentException("Scenario fault IDs must be unique.", nameof(faults));
        }

        foreach (var definition in definitions)
        {
            if (!_applicators.ContainsKey(definition.FaultTypeId))
            {
                throw new KeyNotFoundException($"No bound applicator exists for fault type '{definition.FaultTypeId}'.");
            }

            ValidateTrigger(definition.Activation, definition.FaultId);
            if (definition.Deactivation is not null)
            {
                ValidateTrigger(definition.Deactivation, definition.FaultId);
            }

            ValidateLogicalStepNotBeforeInitial(definition.Activation, definition.FaultId, "activation", nameof(faults));
            if (definition.Deactivation is not null)
            {
                ValidateLogicalStepNotBeforeInitial(definition.Deactivation, definition.FaultId, "deactivation", nameof(faults));
            }
        }

        _faults = definitions.Select(static definition => new RuntimeFaultState(definition)).ToArray();
    }

    public long LogicalStep => _inner.LogicalStep;

    public ControlRoomSnapshot CreatePresentationSnapshot(ControlRoomRunState runState)
    {
        var committed = _inner.CreatePresentationSnapshot(runState);
        EnsureInitialized(committed);
        return committed.WithFaultState(CreateFaultSnapshot());
    }

    public ControlRoomSnapshot Step(ControlRoomRunState runState)
    {
        var committed = _inner.CreatePresentationSnapshot(runState);
        EnsureInitialized(committed);
        var nextLogicalStep = checked(_inner.LogicalStep + 1);
        ApplyTransitions(nextLogicalStep, committed);
        var stepped = _inner.Step(runState);
        return stepped.WithFaultState(CreateFaultSnapshot());
    }

    public void QueueOperatorCommand(ControlRoomCommand command)
        => _inner.QueueOperatorCommand(command);

    public PlantControlAuthorityPresentationSnapshot CreateAutomationSnapshot()
        => RequireAutomationRuntime().CreateAutomationSnapshot();

    public void RequestPlantControlAuthority(PlantControlAuthorityMode mode)
        => RequireAutomationRuntime().RequestPlantControlAuthority(mode);

    public void RequestSupervisoryObjective(SupervisoryObjectiveRequest objective)
        => RequireAutomationRuntime().RequestSupervisoryObjective(objective);

    private IPlantControlAuthorityRuntimeEngine RequireAutomationRuntime()
        => _inner as IPlantControlAuthorityRuntimeEngine
            ?? throw new InvalidOperationException("The wrapped runtime does not expose the M10.5/M10.6 plant-control-authority seam.");

    private void EnsureInitialized(ControlRoomSnapshot committed)
    {
        if (_initialized)
        {
            return;
        }

        ApplyTransitions(_inner.LogicalStep, committed);
        _initialized = true;
    }

    private void ApplyTransitions(long boundaryLogicalStep, ControlRoomSnapshot committed)
    {
        foreach (var state in _faults.Where(static state => state.Lifecycle == ScenarioFaultLifecycleState.Active))
        {
            if (state.Definition.Deactivation is not null
                && IsTriggered(state.Definition.Deactivation, boundaryLogicalStep, committed))
            {
                var applicator = _applicators[state.Definition.FaultTypeId];
                applicator.Deactivate(state.Definition);
                state.Lifecycle = ScenarioFaultLifecycleState.Cleared;
                state.ClearedLogicalStep = boundaryLogicalStep;
                state.LastTransitionSequence = checked(++_transitionSequence);
            }
        }

        foreach (var state in _faults.Where(static state => state.Lifecycle == ScenarioFaultLifecycleState.Pending))
        {
            if (IsTriggered(state.Definition.Activation, boundaryLogicalStep, committed))
            {
                var applicator = _applicators[state.Definition.FaultTypeId];
                applicator.Activate(state.Definition);
                state.Lifecycle = ScenarioFaultLifecycleState.Active;
                state.ActivatedLogicalStep = boundaryLogicalStep;
                state.LastTransitionSequence = checked(++_transitionSequence);
            }
        }
    }

    private bool IsTriggered(
        ScenarioFaultTriggerDefinition trigger,
        long boundaryLogicalStep,
        ControlRoomSnapshot committed)
        => trigger.Kind switch
        {
            ScenarioFaultTriggerKind.LogicalStep => trigger.LogicalStep == boundaryLogicalStep,
            ScenarioFaultTriggerKind.PlantCondition => _conditions.Resolve(trigger.ConditionId!).IsSatisfied(committed),
            _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger.Kind, "Unsupported fault trigger kind."),
        };

    private void ValidateLogicalStepNotBeforeInitial(
        ScenarioFaultTriggerDefinition trigger,
        string faultId,
        string transitionName,
        string argumentName)
    {
        if (trigger.Kind == ScenarioFaultTriggerKind.LogicalStep
            && trigger.LogicalStep!.Value < _inner.LogicalStep)
        {
            throw new ArgumentException(
                $"Fault '{faultId}' {transitionName} step {trigger.LogicalStep.Value} precedes the loaded initial logical step {_inner.LogicalStep}.",
                argumentName);
        }
    }

    private void ValidateTrigger(ScenarioFaultTriggerDefinition trigger, string faultId)
    {
        if (trigger.Kind == ScenarioFaultTriggerKind.PlantCondition)
        {
            try
            {
                _conditions.Resolve(trigger.ConditionId!);
            }
            catch (KeyNotFoundException exception)
            {
                throw new InvalidOperationException(
                    $"Fault '{faultId}' references unknown plant condition '{trigger.ConditionId}'.",
                    exception);
            }
        }
    }

    private ControlRoomFaultStateSnapshot CreateFaultSnapshot()
        => new(_faults.Select(static state => new ControlRoomFaultStatusSnapshot(
            state.Definition.FaultId,
            state.Definition.FaultTypeId,
            state.Definition.TargetId,
            state.Lifecycle,
            state.ActivatedLogicalStep,
            state.ClearedLogicalStep,
            state.LastTransitionSequence)));
}
