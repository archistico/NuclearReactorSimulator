namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

public sealed record MissionPerformanceTimelineProjection(
    IReadOnlyList<MissionPerformanceTimelineEntrySnapshot> LifecycleSpine,
    IReadOnlyList<MissionPerformanceTimelineEntrySnapshot> RecentOperationalEvidence,
    IReadOnlyList<MissionPerformanceTimelineEntrySnapshot> Timeline);
