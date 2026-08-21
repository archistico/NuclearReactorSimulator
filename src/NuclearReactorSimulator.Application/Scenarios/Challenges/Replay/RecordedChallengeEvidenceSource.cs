using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges.Replay;

/// <summary>
/// Internal read-only replay adapter that feeds recorded deterministic frames/actions through the exact M10.9.6.1
/// lifecycle tracker. It owns no plant session, command dispatcher or mutable Simulation state.
/// </summary>
internal sealed class RecordedChallengeEvidenceSource : IChallengeEvidenceSource
{
    private readonly List<ScenarioOperatorActionRecord> _acceptedOperatorActions = new();

    public RecordedChallengeEvidenceSource(ControlRoomSnapshot initialSnapshot)
    {
        Current = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
    }

    public ControlRoomSnapshot Current { get; private set; }

    public IReadOnlyList<ScenarioOperatorActionRecord> AcceptedOperatorActions
        => Array.AsReadOnly(_acceptedOperatorActions.ToArray());

    public event EventHandler<ControlRoomSnapshotChangedEventArgs>? DeterministicStepCompleted;
    public event EventHandler<ScenarioOperatorActionAcceptedEventArgs>? OperatorActionAccepted;

    public void Accept(ScenarioOperatorActionRecord action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action.LogicalStep != Current.LogicalStep)
        {
            throw new InvalidOperationException(
                $"Recorded action logical step {action.LogicalStep} does not match current replay step {Current.LogicalStep}.");
        }
        var expectedSequence = checked((long)_acceptedOperatorActions.Count + 1L);
        if (action.Sequence != expectedSequence)
        {
            throw new InvalidOperationException(
                $"Recorded action sequence {action.Sequence} is not the expected deterministic sequence {expectedSequence}.");
        }

        _acceptedOperatorActions.Add(action);
        OperatorActionAccepted?.Invoke(this, new ScenarioOperatorActionAcceptedEventArgs(action));
    }

    public void Advance(ControlRoomSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var expectedStep = checked(Current.LogicalStep + 1L);
        if (snapshot.LogicalStep != expectedStep)
        {
            throw new InvalidOperationException(
                $"Recorded challenge replay requires contiguous logical steps; expected {expectedStep}, received {snapshot.LogicalStep}.");
        }

        Current = snapshot;
        DeterministicStepCompleted?.Invoke(this, new ControlRoomSnapshotChangedEventArgs(snapshot));
    }
}
