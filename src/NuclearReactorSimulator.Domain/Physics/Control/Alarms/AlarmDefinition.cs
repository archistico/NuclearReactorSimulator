namespace NuclearReactorSimulator.Domain.Physics.Control.Alarms;

/// <summary>One operator-facing alarm definition. Alarm semantics never own a physical protection action.</summary>
public sealed record AlarmDefinition
{
    public AlarmDefinition(
        string id,
        string title,
        AlarmSeverity severity,
        AlarmLatchingMode latchingMode,
        AlarmConditionDefinition condition,
        string? firstOutGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Alarm id and title must be non-empty.");
        }
        if (!Enum.IsDefined(typeof(AlarmSeverity), severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown alarm severity.");
        }
        if (!Enum.IsDefined(typeof(AlarmLatchingMode), latchingMode))
        {
            throw new ArgumentOutOfRangeException(nameof(latchingMode), latchingMode, "Unknown alarm latching mode.");
        }
        Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        var canonicalGroup = string.IsNullOrWhiteSpace(firstOutGroupId) ? null : firstOutGroupId.Trim();
        if (canonicalGroup is not null && latchingMode != AlarmLatchingMode.LatchedUntilReset)
        {
            throw new ArgumentException("First-out alarms must be latched until explicit reset.", nameof(firstOutGroupId));
        }

        Id = id.Trim();
        Title = title.Trim();
        Severity = severity;
        LatchingMode = latchingMode;
        FirstOutGroupId = canonicalGroup;
    }

    public string Id { get; }
    public string Title { get; }
    public AlarmSeverity Severity { get; }
    public AlarmLatchingMode LatchingMode { get; }
    public AlarmConditionDefinition Condition { get; }
    public string? FirstOutGroupId { get; }
}
