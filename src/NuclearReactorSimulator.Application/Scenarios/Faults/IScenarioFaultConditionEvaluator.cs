using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.Scenarios.Faults;

/// <summary>Evaluates one named deterministic plant condition from the committed UI-safe snapshot only.</summary>
public interface IScenarioFaultConditionEvaluator
{
    string ConditionId { get; }

    bool IsSatisfied(ControlRoomSnapshot snapshot);
}
