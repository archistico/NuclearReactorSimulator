namespace NuclearReactorSimulator.Simulation.Physics.Fluids;

/// <summary>
/// Selects the simplified water/steam inverse-domain closure used by diagnostic and production composition.
/// HistoricalCorrelationTopology preserves the pre-I.5 REV1 behavior exactly. CorrelationConsistentInverseDomain
/// is the opt-in repair candidate that anchors superheated vapor to the correlated saturated-vapor boundary and
/// discovers the complete saturated temperature interval around the low-temperature density maximum.
/// </summary>
public enum WaterSteamThermodynamicClosureMode
{
    HistoricalCorrelationTopology = 0,
    CorrelationConsistentInverseDomain = 1,

    /// <summary>
    /// Explicit opt-in RP1C-selected C4 repair backed by the versioned embedded NRSVR2C4 reference payload.
    /// Existing modes and default construction remain unchanged.
    /// </summary>
    ReferenceConsistentTabulatedInverseDomain = 2,
}
