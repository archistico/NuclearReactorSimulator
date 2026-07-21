using NuclearReactorSimulator.Domain.Physics.Control.Alarms;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;
using NuclearReactorSimulator.Simulation.Physics.Control.Protection;
using NuclearReactorSimulator.Simulation.Physics.Instrumentation;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

/// <summary>
/// Deterministic M5.6 alarm/annunciator state machine. It observes measured signals and M5.5 protection state only;
/// acknowledgement and reset never feed back into protection or plant physics.
/// </summary>
public sealed class AlarmSystemSolver
{
    private readonly AlarmSystemDefinition _definition;

    public AlarmSystemSolver(AlarmSystemDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public AlarmSystemStepResult Step(
        MeasuredSignalFrame measuredSignals,
        ProtectionSystemSnapshot protection,
        AlarmSystemState committedState,
        AlarmSystemInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(measuredSignals);
        ArgumentNullException.ThrowIfNull(protection);
        ArgumentNullException.ThrowIfNull(committedState);
        ArgumentNullException.ThrowIfNull(inputs);
        if (!ReferenceEquals(measuredSignals.Definition, _definition.Instrumentation))
        {
            throw new ArgumentException("Measured signals do not use the alarm system's canonical instrumentation definition.", nameof(measuredSignals));
        }
        if (!ReferenceEquals(protection.Definition, _definition.Protection))
        {
            throw new ArgumentException("Protection snapshot does not use the alarm system's canonical M5.5 protection definition.", nameof(protection));
        }
        if (!ReferenceEquals(committedState.Definition, _definition) || !ReferenceEquals(inputs.Definition, _definition))
        {
            throw new ArgumentException("Alarm state and inputs must use this solver's canonical definition.");
        }

        var nextSequence = committedState.NextEventSequence;
        var events = new List<AlarmEventSnapshot>();
        var work = _definition.Alarms.Select(alarm =>
        {
            var committed = committedState.GetChannel(alarm.Id);
            var active = EvaluateCondition(alarm.Condition, measuredSignals, protection);
            var item = new MutableAlarm(alarm, committed, active);
            if (active && !committed.ConditionActive)
            {
                item.IsAcknowledged = false;
                item.ActivationSequence = AddEvent(events, ref nextSequence, alarm.Id, AlarmEventKind.Activated);
                if (alarm.LatchingMode == AlarmLatchingMode.LatchedUntilReset)
                {
                    item.IsLatched = true;
                }
            }
            else if (!active && committed.ConditionActive)
            {
                _ = AddEvent(events, ref nextSequence, alarm.Id, AlarmEventKind.Cleared);
                if (alarm.LatchingMode == AlarmLatchingMode.NonLatching)
                {
                    item.IsAcknowledged = false;
                    item.ActivationSequence = null;
                    item.IsFirstOut = false;
                }
            }
            return item;
        }).ToArray();

        foreach (var item in work)
        {
            item.AcknowledgeRequested = inputs.IsAcknowledgeRequested(item.Definition.Id);
            if (item.AcknowledgeRequested && item.IsAnnunciated && !item.IsAcknowledged)
            {
                item.IsAcknowledged = true;
                item.AcknowledgeApplied = true;
                _ = AddEvent(events, ref nextSequence, item.Definition.Id, AlarmEventKind.Acknowledged);
            }
        }

        foreach (var item in work)
        {
            item.ResetRequested = inputs.IsResetRequested(item.Definition.Id);
            if (item.ResetRequested
                && item.Definition.LatchingMode == AlarmLatchingMode.LatchedUntilReset
                && item.IsLatched
                && !item.ConditionActive
                && item.IsAcknowledged)
            {
                item.IsLatched = false;
                item.IsAcknowledged = false;
                item.IsFirstOut = false;
                item.ActivationSequence = null;
                item.ResetAccepted = true;
                _ = AddEvent(events, ref nextSequence, item.Definition.Id, AlarmEventKind.Reset);
            }
        }

        AssignFirstOut(work);

        var candidateChannels = work.Select(item => new AlarmChannelState(
            item.Definition.Id,
            item.ConditionActive,
            item.IsLatched,
            item.IsAcknowledged,
            item.IsFirstOut,
            item.ActivationSequence)).ToArray();
        var candidateState = new AlarmSystemState(_definition, candidateChannels, nextSequence);
        var snapshots = work.Select(item => new AlarmSnapshot(
            item.Definition.Id,
            item.Definition.Title,
            item.Definition.Severity,
            item.Definition.FirstOutGroupId,
            item.ConditionActive,
            item.IsLatched,
            item.IsAcknowledged,
            item.IsAnnunciated,
            item.IsFirstOut,
            item.ActivationSequence,
            ResolveAnnunciatorState(item),
            item.AcknowledgeRequested,
            item.AcknowledgeApplied,
            item.ResetRequested,
            item.ResetAccepted)).ToArray();
        var groups = _definition.Alarms.Where(static item => item.FirstOutGroupId is not null)
            .GroupBy(static item => item.FirstOutGroupId!, StringComparer.Ordinal)
            .Select(group =>
            {
                var members = snapshots.Where(item => string.Equals(item.FirstOutGroupId, group.Key, StringComparison.Ordinal)).ToArray();
                return new AlarmFirstOutGroupSnapshot(
                    group.Key,
                    members.FirstOrDefault(static item => item.IsFirstOut)?.AlarmId,
                    members.Where(static item => item.IsAnnunciated).Select(static item => item.AlarmId));
            }).ToArray();
        var snapshot = new AlarmSystemSnapshot(_definition, snapshots, groups, events);
        return new AlarmSystemStepResult(candidateState, snapshot);
    }

    private static bool EvaluateCondition(
        AlarmConditionDefinition condition,
        MeasuredSignalFrame measuredSignals,
        ProtectionSystemSnapshot protection)
        => condition switch
        {
            MeasuredAlarmConditionDefinition measured => EvaluateMeasured(measured, measuredSignals.GetSignal(measured.MeasurementChannelId)),
            ProtectionFunctionAlarmConditionDefinition function => protection.Functions.First(item => string.Equals(item.FunctionId, function.ProtectionFunctionId, StringComparison.Ordinal)).IsLatched,
            ProtectionActionAlarmConditionDefinition action => (protection.LatchedActions & action.Action) != ProtectionAction.None,
            ProtectionInterlockAlarmConditionDefinition interlock => (protection.ActiveInterlocks & interlock.Action) != ProtectionInterlockAction.None,
            _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, "Unsupported alarm condition definition."),
        };

    private static bool EvaluateMeasured(MeasuredAlarmConditionDefinition definition, MeasuredSignal signal)
    {
        if (signal.Validity != SignalValidity.Valid || !signal.EngineeringValue.HasValue)
        {
            return definition.ActiveOnInvalidMeasurement;
        }
        return definition.Comparison == AlarmComparison.High
            ? signal.EngineeringValue.Value >= definition.Threshold
            : signal.EngineeringValue.Value <= definition.Threshold;
    }

    private static long AddEvent(List<AlarmEventSnapshot> events, ref long nextSequence, string alarmId, AlarmEventKind kind)
    {
        var sequence = nextSequence++;
        events.Add(new AlarmEventSnapshot(sequence, alarmId, kind));
        return sequence;
    }

    private static void AssignFirstOut(IEnumerable<MutableAlarm> alarms)
    {
        foreach (var group in alarms.Where(static item => item.Definition.FirstOutGroupId is not null)
                     .GroupBy(static item => item.Definition.FirstOutGroupId!, StringComparer.Ordinal))
        {
            var existing = group.FirstOrDefault(static item => item.IsFirstOut && item.IsAnnunciated);
            if (existing is not null)
            {
                foreach (var other in group.Where(item => !ReferenceEquals(item, existing)))
                {
                    other.IsFirstOut = false;
                }
                continue;
            }

            foreach (var item in group)
            {
                item.IsFirstOut = false;
            }
            var candidate = group.Where(static item => item.IsAnnunciated && item.ActivationSequence.HasValue)
                .OrderBy(static item => item.ActivationSequence)
                .ThenBy(static item => item.Definition.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (candidate is not null)
            {
                candidate.IsFirstOut = true;
            }
        }
    }

    private static AlarmAnnunciatorState ResolveAnnunciatorState(MutableAlarm item)
    {
        if (!item.IsAnnunciated)
        {
            return AlarmAnnunciatorState.Normal;
        }
        if (item.ConditionActive)
        {
            return item.IsAcknowledged ? AlarmAnnunciatorState.ActiveAcknowledged : AlarmAnnunciatorState.ActiveUnacknowledged;
        }
        return item.IsAcknowledged ? AlarmAnnunciatorState.ReturnedAcknowledged : AlarmAnnunciatorState.ReturnedUnacknowledged;
    }

    private sealed class MutableAlarm
    {
        public MutableAlarm(AlarmDefinition definition, AlarmChannelState committed, bool conditionActive)
        {
            Definition = definition;
            ConditionActive = conditionActive;
            IsLatched = committed.IsLatched;
            IsAcknowledged = committed.IsAcknowledged;
            IsFirstOut = committed.IsFirstOut;
            ActivationSequence = committed.ActivationSequence;
        }

        public AlarmDefinition Definition { get; }
        public bool ConditionActive { get; }
        public bool IsLatched { get; set; }
        public bool IsAcknowledged { get; set; }
        public bool IsFirstOut { get; set; }
        public long? ActivationSequence { get; set; }
        public bool AcknowledgeRequested { get; set; }
        public bool AcknowledgeApplied { get; set; }
        public bool ResetRequested { get; set; }
        public bool ResetAccepted { get; set; }
        public bool IsAnnunciated => ConditionActive || IsLatched;
    }
}
