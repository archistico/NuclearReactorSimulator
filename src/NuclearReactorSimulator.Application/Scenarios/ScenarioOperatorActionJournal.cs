using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios;

/// <summary>
/// Bounded-by-session logical journal of operator actions that passed scenario gating and were accepted by the runtime
/// command boundary. It is application/training state only and never participates in physical integration.
/// </summary>
public sealed class ScenarioOperatorActionJournal
{
    private readonly object _gate = new();
    private readonly List<ScenarioOperatorActionRecord> _actions = new();
    private long _nextSequence = 1;

    public event EventHandler<ScenarioOperatorActionAcceptedEventArgs>? ActionAccepted;

    public IReadOnlyList<ScenarioOperatorActionRecord> Actions
    {
        get
        {
            lock (_gate)
            {
                return Array.AsReadOnly(_actions.ToArray());
            }
        }
    }

    internal void RecordAccepted(long logicalStep, ControlRoomCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ScenarioOperatorActionRecord record;

        lock (_gate)
        {
            record = new ScenarioOperatorActionRecord(_nextSequence++, logicalStep, command);
            _actions.Add(record);
        }

        ActionAccepted?.Invoke(this, new ScenarioOperatorActionAcceptedEventArgs(record));
    }
}
