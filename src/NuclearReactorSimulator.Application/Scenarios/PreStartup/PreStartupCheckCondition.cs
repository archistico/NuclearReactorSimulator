namespace NuclearReactorSimulator.Application.Scenarios.PreStartup;

/// <summary>
/// Presentation-observable readiness conditions used by M7.2. These conditions never inspect authoritative plant state
/// directly and never mutate the simulation to force readiness.
/// </summary>
public enum PreStartupCheckCondition
{
    MeasuredSignalsHealthy = 0,
    ProtectionClear = 1,
    ReactorShutdown = 2,
    ControlRodsInserted = 3,
    MainCirculationPumpsStopped = 4,
    MainCirculationPumpsRunning = 5,
    TurbineStopped = 6,
    GeneratorBreakersOpen = 7,
    SteamIsolationClosed = 8,
    NoAnnunciatedAlarms = 9,
}
