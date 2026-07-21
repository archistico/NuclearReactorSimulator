using NuclearReactorSimulator.Domain.Physics.Control.Alarms;

namespace NuclearReactorSimulator.Simulation.Physics.Control.Alarms;

public sealed class AlarmSystemInputs
{
    private readonly HashSet<string> _acknowledgeAlarmIds;
    private readonly HashSet<string> _resetAlarmIds;

    public AlarmSystemInputs(
        AlarmSystemDefinition definition,
        IEnumerable<string>? acknowledgeAlarmIds = null,
        IEnumerable<string>? resetAlarmIds = null,
        bool acknowledgeAll = false,
        bool resetAll = false)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _acknowledgeAlarmIds = Canonicalize(definition, acknowledgeAlarmIds, nameof(acknowledgeAlarmIds));
        _resetAlarmIds = Canonicalize(definition, resetAlarmIds, nameof(resetAlarmIds));
        AcknowledgeAll = acknowledgeAll;
        ResetAll = resetAll;
    }

    public AlarmSystemDefinition Definition { get; }
    public bool AcknowledgeAll { get; }
    public bool ResetAll { get; }
    public IReadOnlySet<string> AcknowledgeAlarmIds => _acknowledgeAlarmIds;
    public IReadOnlySet<string> ResetAlarmIds => _resetAlarmIds;

    public bool IsAcknowledgeRequested(string alarmId) => AcknowledgeAll || _acknowledgeAlarmIds.Contains(alarmId);
    public bool IsResetRequested(string alarmId) => ResetAll || _resetAlarmIds.Contains(alarmId);

    private static HashSet<string> Canonicalize(AlarmSystemDefinition definition, IEnumerable<string>? ids, string parameterName)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in ids ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new ArgumentException("Alarm command ids cannot be empty or whitespace.", parameterName);
            }
            var id = raw.Trim();
            _ = definition.GetAlarm(id);
            result.Add(id);
        }
        return result;
    }
}
