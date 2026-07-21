namespace NuclearReactorSimulator.Domain.Physics.Control.TurbineSecondary;

/// <summary>Plant-specific M5.4 turbine/steam/feedwater control-loop roles.</summary>
public enum TurbineSecondaryControlLoopKind
{
    TurbineSpeedAdmission = 0,
    TurbineLoadAdmission = 1,
    SteamPressureAdmission = 2,
    SteamDrumLevelFeedwater = 3,
    HotwellInventoryCondensate = 4,
}
