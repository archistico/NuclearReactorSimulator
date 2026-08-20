using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>
/// Adapter exposing only deterministic read-side session evidence to the challenge tracker. The tracker therefore cannot
/// dispatch commands or mutate plant-control authority through this seam.
/// </summary>
public sealed class ScenarioChallengeEvidenceSource : IChallengeEvidenceSource
{
    private readonly ControlRoomRuntimeCoordinator _coordinator;
    private readonly ScenarioOperatorActionJournal _operatorActions;

    public ScenarioChallengeEvidenceSource(ScenarioSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _coordinator = session.Coordinator;
        _operatorActions = session.OperatorActions;
    }

    public ControlRoomSnapshot Current => _coordinator.Current;
    public IReadOnlyList<ScenarioOperatorActionRecord> AcceptedOperatorActions => _operatorActions.Actions;

    public event EventHandler<ControlRoomSnapshotChangedEventArgs>? DeterministicStepCompleted
    {
        add => _coordinator.DeterministicStepCompleted += value;
        remove => _coordinator.DeterministicStepCompleted -= value;
    }

    public event EventHandler<ScenarioOperatorActionAcceptedEventArgs>? OperatorActionAccepted
    {
        add => _operatorActions.ActionAccepted += value;
        remove => _operatorActions.ActionAccepted -= value;
    }
}
