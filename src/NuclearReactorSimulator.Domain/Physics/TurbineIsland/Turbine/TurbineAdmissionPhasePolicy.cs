namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Turbine;

/// <summary>
/// Defines how a turbine stage treats non-vapor mass at its admission boundary.
/// LegacyUnrestricted preserves the historical total-mixture transfer semantics.
/// VaporMassFractionLimited admits only the committed vapor mass fraction, preventing liquid from becoming a zero-work bypass.
/// </summary>
public enum TurbineAdmissionPhasePolicy
{
    LegacyUnrestricted = 0,
    VaporMassFractionLimited = 1,
}
