using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// One accepted M10.5/M10.6 semantic authority/objective intent. It is replay input, not physical state, and applies on the
/// next deterministic fixed step just like accepted operator commands at the same committed logical step.
/// </summary>
public sealed record ScenarioAutomationIntentRecord
{
    public ScenarioAutomationIntentRecord(
        long sequence,
        long logicalStep,
        ScenarioAutomationIntentKind kind,
        PlantControlAuthorityMode? authority,
        SupervisoryObjectiveRequest? objective)
    {
        if (sequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }
        if (logicalStep < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(logicalStep));
        }
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }
        if ((kind == ScenarioAutomationIntentKind.PlantControlAuthority) != authority.HasValue
            || (kind == ScenarioAutomationIntentKind.SupervisoryObjective) != (objective is not null))
        {
            throw new ArgumentException("Automation intent payload must match its semantic kind.");
        }
        if (authority.HasValue && !Enum.IsDefined(authority.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(authority));
        }

        Sequence = sequence;
        LogicalStep = logicalStep;
        Kind = kind;
        Authority = authority;
        Objective = objective;
    }

    public long Sequence { get; }
    public long LogicalStep { get; }
    public ScenarioAutomationIntentKind Kind { get; }
    public PlantControlAuthorityMode? Authority { get; }
    public SupervisoryObjectiveRequest? Objective { get; }
}
