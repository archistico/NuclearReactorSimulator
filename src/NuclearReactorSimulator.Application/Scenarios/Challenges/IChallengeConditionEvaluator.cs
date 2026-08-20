using NuclearReactorSimulator.Application.ControlRoom;
using NuclearReactorSimulator.Application.Scenarios;

namespace NuclearReactorSimulator.Application.Scenarios.Challenges;

/// <summary>
/// Read-only deterministic challenge evaluator. Implementations may inspect immutable presentation snapshots and accepted
/// scenario operator-action history only; they receive no command dispatcher or simulation-state mutator.
/// </summary>
public interface IChallengeConditionEvaluator
{
    ChallengeConditionObservation Evaluate(
        ChallengeConditionDefinition condition,
        ControlRoomSnapshot snapshot,
        IReadOnlyList<ScenarioOperatorActionRecord> acceptedOperatorActions);
}
