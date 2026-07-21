namespace NuclearReactorSimulator.Application.Scenarios.Operations;

public enum PowerManoeuvringCheckCondition
{
    MeasuredSignalsHealthy = 0,
    ProtectionClear = 1,
    MainCirculationPumpsRunning = 2,
    GeneratorBreakersClosed = 3,
    StableLowLoadParallelOperation = 4,
    IncreasedElectricalLoadEstablished = 5,
    ReducedElectricalLoadEstablished = 6,
    TemperatureFeedbackObservable = 7,
    VoidFeedbackObservable = 8,
    XenonBoundaryExplicit = 9,
    GeneratorUnloaded = 10,
    GeneratorBreakersOpen = 11,
    ReactorShutdownEstablished = 12,
    PostShutdownCoolingEstablished = 13,
}
