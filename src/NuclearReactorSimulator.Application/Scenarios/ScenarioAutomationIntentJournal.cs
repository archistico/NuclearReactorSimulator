using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>Session-bounded deterministic journal of accepted M10.5/M10.6 authority/objective intents.</summary>
public sealed class ScenarioAutomationIntentJournal
{
    private readonly object _gate = new();
    private readonly List<ScenarioAutomationIntentRecord> _intents = new();
    private long _nextSequence = 1;

    public IReadOnlyList<ScenarioAutomationIntentRecord> Intents
    {
        get
        {
            lock (_gate)
            {
                return Array.AsReadOnly(_intents.ToArray());
            }
        }
    }

    internal void RecordAuthority(long logicalStep, PlantControlAuthorityMode authority)
    {
        lock (_gate)
        {
            _intents.Add(new ScenarioAutomationIntentRecord(
                _nextSequence++,
                logicalStep,
                ScenarioAutomationIntentKind.PlantControlAuthority,
                authority,
                null));
        }
    }

    internal void RecordObjective(long logicalStep, SupervisoryObjectiveRequest objective)
    {
        ArgumentNullException.ThrowIfNull(objective);
        lock (_gate)
        {
            _intents.Add(new ScenarioAutomationIntentRecord(
                _nextSequence++,
                logicalStep,
                ScenarioAutomationIntentKind.SupervisoryObjective,
                null,
                objective));
        }
    }
}
