using NuclearReactorSimulator.Application.ControlRoom.Automation;
using NuclearReactorSimulator.Application.Scenarios.Challenges;
using NuclearReactorSimulator.Application.Scenarios.Training;
using NuclearReactorSimulator.Domain.Physics.Control.Supervisory;

namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

/// <summary>
/// Immutable M10.9.7.1 mission/performance read model. It aggregates canonical challenge, demand, score and control-mode
/// owners without duplicating their semantics or introducing plant command authority.
/// </summary>
public sealed record MissionPerformanceSnapshot(
    string PackExactId,
    string ChallengeExactId,
    string ScenarioId,
    string ObjectiveId,
    string ObjectiveTitle,
    string ObjectiveDescription,
    ChallengeLifecycleState LifecycleState,
    long LogicalStep,
    long? ActivatedLogicalStep,
    long? ElapsedLogicalSteps,
    long? TerminalLogicalStep,
    long? TargetWindowStartLogicalStep,
    long? TargetWindowEndLogicalStep,
    long? HardFailureDeadlineLogicalStep,
    MissionPerformanceDemandSnapshot Demand,
    MissionPerformanceScoreSnapshot Score,
    IReadOnlyList<MissionPerformanceEventSnapshot> RecentEvents,
    TrainingGuidanceMode AssistanceMode,
    bool PlantControlAuthorityAvailable,
    PlantControlAuthorityMode? RequestedControlAuthority,
    PlantControlAuthorityMode? EffectiveControlAuthority,
    PlantControlAuthorityHealth? ControlAuthorityHealth,
    string? ControlAuthorityDegradationReason);
