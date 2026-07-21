using System.Collections.ObjectModel;
using NuclearReactorSimulator.Domain.Physics.Control.Protection;
using NuclearReactorSimulator.Domain.Physics.Instrumentation;

namespace NuclearReactorSimulator.Domain.Physics.Control.Alarms;

/// <summary>Canonical M5.6 alarm/annunciator definition over M5.1 measurements and M5.5 protection observations.</summary>
public sealed class AlarmSystemDefinition
{
    public AlarmSystemDefinition(
        string id,
        InstrumentationSystemDefinition instrumentation,
        ProtectionSystemDefinition protection,
        IEnumerable<AlarmDefinition> alarms)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Alarm-system id cannot be empty or whitespace.", nameof(id));
        }
        Instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
        Protection = protection ?? throw new ArgumentNullException(nameof(protection));
        ArgumentNullException.ThrowIfNull(alarms);
        if (!ReferenceEquals(instrumentation, protection.Instrumentation))
        {
            throw new ArgumentException("M5.6 alarms and M5.5 protection must observe the same canonical instrumentation definition.");
        }

        var canonical = alarms.Select(item => item ?? throw new ArgumentException("Alarm definitions cannot contain null entries.", nameof(alarms)))
            .OrderBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();
        if (canonical.Length == 0)
        {
            throw new ArgumentException("An alarm system must contain at least one alarm.", nameof(alarms));
        }
        var duplicate = canonical.GroupBy(static item => item.Id, StringComparer.Ordinal).FirstOrDefault(static group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"Duplicate alarm id '{duplicate.Key}'.", nameof(alarms));
        }

        foreach (var alarm in canonical)
        {
            ValidateCondition(alarm.Condition);
        }

        Id = id.Trim();
        Alarms = new ReadOnlyCollection<AlarmDefinition>(canonical);
    }

    public string Id { get; }
    public InstrumentationSystemDefinition Instrumentation { get; }
    public ProtectionSystemDefinition Protection { get; }
    public IReadOnlyList<AlarmDefinition> Alarms { get; }

    public AlarmDefinition GetAlarm(string id)
        => Alarms.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal))
            ?? throw new KeyNotFoundException($"Unknown alarm '{id}'.");

    private void ValidateCondition(AlarmConditionDefinition condition)
    {
        switch (condition)
        {
            case MeasuredAlarmConditionDefinition measured:
                _ = Instrumentation.GetChannel(measured.MeasurementChannelId);
                break;
            case ProtectionFunctionAlarmConditionDefinition protectionFunction:
                _ = Protection.GetTripFunction(protectionFunction.ProtectionFunctionId);
                break;
            case ProtectionActionAlarmConditionDefinition:
                break;
            case ProtectionInterlockAlarmConditionDefinition interlock:
                if (!Protection.Interlocks.Any(item => (item.Actions & interlock.Action) != ProtectionInterlockAction.None))
                {
                    throw new ArgumentException($"No canonical M5.5 interlock exposes action '{interlock.Action}'.");
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(condition), condition, "Unsupported alarm condition definition.");
        }
    }
}
