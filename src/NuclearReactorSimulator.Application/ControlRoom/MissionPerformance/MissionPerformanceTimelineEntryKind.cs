namespace NuclearReactorSimulator.Application.ControlRoom.MissionPerformance;

public enum MissionPerformanceTimelineEntryKind
{
    Objective = 0,
    Demand = 1,
    OperatorAction = 2,
    Alarm = 3,
    Protection = 4,
    Fault = 5,
    Scoring = 6,
}
