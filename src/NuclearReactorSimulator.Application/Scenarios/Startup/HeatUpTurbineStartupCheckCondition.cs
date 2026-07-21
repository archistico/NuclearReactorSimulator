namespace NuclearReactorSimulator.Application.Scenarios.Startup;

public enum HeatUpTurbineStartupCheckCondition
{
    MeasuredSignalsHealthy = 0,
    ProtectionClear = 1,
    MainCirculationPumpsRunning = 2,
    ReactorHeatingPowerEstablished = 3,
    SteamRaisingPressureEstablished = 4,
    SteamDrumInventoryAvailable = 5,
    TurbineStartupLineupReady = 6,
    TurbineStopped = 7,
    TurbineRolling = 8,
    TurbineWarmupSpeedBand = 9,
    TurbineNearSynchronousSpeed = 10,
    GeneratorBreakersOpen = 11,
    GeneratorUnloaded = 12,
}
