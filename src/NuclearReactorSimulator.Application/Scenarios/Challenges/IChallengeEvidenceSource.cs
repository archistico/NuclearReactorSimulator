using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>Read-only deterministic evidence seam used by challenge lifecycle tracking.</summary>
public interface IChallengeEvidenceSource
{
    ControlRoomSnapshot Current { get; }
    IReadOnlyList<ScenarioOperatorActionRecord> AcceptedOperatorActions { get; }
    event EventHandler<ControlRoomSnapshotChangedEventArgs>? DeterministicStepCompleted;
    event EventHandler<ScenarioOperatorActionAcceptedEventArgs>? OperatorActionAccepted;
}
