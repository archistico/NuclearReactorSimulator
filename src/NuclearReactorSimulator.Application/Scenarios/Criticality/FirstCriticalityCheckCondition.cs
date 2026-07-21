namespace NuclearReactorSimulator.Application.Scenarios.Criticality;

public enum FirstCriticalityCheckCondition
{
    MeasuredSignalsHealthy = 0,
    ProtectionClear = 1,
    MainCirculationPumpsRunning = 2,
    SteamIsolationClosed = 3,
    GeneratorBreakersOpen = 4,
    RodWithdrawalPermitted = 5,
    SourceRangePowerEstablished = 6,
    ApproachToCriticality = 7,
    CriticalityEstablished = 8,
    LowPowerBand = 9,
    StableLowPowerPeriod = 10,
    XenonBoundaryExplicit = 11,
}
