using NuclearReactorSimulator.Application.Scenarios.Faults;

namespace NuclearReactorSimulator.Application.ControlRoom;

/// <summary>UI-safe M8.1 projection of one declared scenario fault lifecycle.</summary>
public sealed record ControlRoomFaultStatusSnapshot(
    string FaultId,
    string FaultTypeId,
    string TargetId,
    ScenarioFaultLifecycleState Lifecycle,
    long? ActivatedLogicalStep,
    long? ClearedLogicalStep,
    long LastTransitionSequence);
