namespace NuclearReactorSimulator.Domain.Physics.Reactor.PrimaryCircuit.SteamDrums;

/// <summary>
/// Selects how the aggregated steam-drum model closes the liquid-recirculation mass balance.
/// LegacyReturnSplit preserves the historical M3.6 zero-residence split for compatibility only.
/// CirculationDemandBalanced uses committed main-circulation pump demand as the liquid downcomer/recirculation outflow,
/// allowing drum inventory to respond to feedwater-versus-steam imbalance instead of becoming a monotonic feedwater accumulator.
/// </summary>
public enum SteamDrumLiquidRecirculationMode
{
    LegacyReturnSplit = 0,
    CirculationDemandBalanced = 1,
}
