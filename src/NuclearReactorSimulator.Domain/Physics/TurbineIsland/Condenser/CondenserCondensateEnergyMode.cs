namespace NuclearReactorSimulator.Domain.Physics.TurbineIsland.Condenser;

/// <summary>
/// Selects the specific-internal-energy state assigned to mass condensed from the steam space into the hotwell.
/// The legacy mode preserves historical replay/fixture behavior. The pressure-resolved mode closes current-v2
/// phase change against saturated-liquid internal energy at the committed condenser steam-space pressure.
/// </summary>
public enum CondenserCondensateEnergyMode
{
    LegacyHotwellSpecificInternalEnergy = 0,
    SaturatedLiquidAtSteamSpacePressure = 1,
}
