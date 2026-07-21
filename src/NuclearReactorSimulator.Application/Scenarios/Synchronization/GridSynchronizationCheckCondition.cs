namespace NuclearReactorSimulator.Application.Scenarios.Synchronization;

public enum GridSynchronizationCheckCondition
{
    MeasuredSignalsHealthy = 0,
    ProtectionClear = 1,
    MainCirculationPumpsRunning = 2,
    ReactorPowerAvailable = 3,
    TurbineAtSynchronousSpeed = 4,
    SynchronizationWindowSatisfied = 5,
    GeneratorBreakersOpen = 6,
    GeneratorBreakersClosed = 7,
    GeneratorUnloaded = 8,
    InitialElectricalLoadEstablished = 9,
    ReactorPowerSupportsElectricalLoad = 10,
    StableLowLoadHandoff = 11,
}
