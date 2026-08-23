namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

/// <summary>
/// Defines how a turbine stage treats non-vapor mass at its admission boundary.
/// LegacyUnrestricted preserves the historical total-mixture transfer semantics.
/// VaporMassFractionLimited admits only the committed vapor mass fraction and leaves non-vapor inventory upstream.
/// VaporMassFractionLimitedWithMoistureDrain admits only vapor through the work-producing stage while assigning
/// the rejected non-vapor mass to an explicit moisture-drain node.
/// </summary>
public enum TurbineAdmissionPhasePolicy
{
    LegacyUnrestricted = 0,
    VaporMassFractionLimited = 1,
    VaporMassFractionLimitedWithMoistureDrain = 2,
}
