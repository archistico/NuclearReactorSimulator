namespace NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;

/// <summary>
/// Selects which speed reference is allowed to drive integral action while a breaker-closed droop governor is active.
/// </summary>
public enum TurbineGovernorIntegralReferenceMode
{
    /// <summary>
    /// Historical behavior: proportional, integral and derivative terms all use the effective droop-shifted speed reference.
    /// Retained for exact-version compatibility.
    /// </summary>
    EffectiveDroopSetpoint = 0,

    /// <summary>
    /// Breaker-closed droop behavior: proportional/derivative terms retain the droop-shifted reference while integral action
    /// uses synchronous grid speed, preventing integral action from erasing the intentional droop offset.
    /// </summary>
    SynchronousSpeedWhenParalleled = 1,
}
