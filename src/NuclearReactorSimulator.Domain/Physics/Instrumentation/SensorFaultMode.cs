namespace NuclearReactorSimulator.Domain.Physics.Instrumentation;

/// <summary>
/// Deterministic M5.1 fault seam. Scenario scheduling/activation belongs to M8; this enum only defines sensor behavior.
/// </summary>
public enum SensorFaultMode
{
    None = 0,
    Bias = 1,
    Freeze = 2,
    FailedLow = 3,
    FailedHigh = 4,
    Unavailable = 5
}
