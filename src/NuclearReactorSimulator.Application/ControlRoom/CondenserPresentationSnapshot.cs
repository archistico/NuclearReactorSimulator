using System.Text.Json.Serialization;

namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record CondenserPresentationSnapshot(
    string CondenserId,
    string TurbineStageGroupId,
    ControlRoomValueSnapshot Pressure,
    ControlRoomValueSnapshot Vacuum,
    ControlRoomValueSnapshot HotwellMass,
    ControlRoomValueSnapshot CondensationFlow,
    ControlRoomValueSnapshot HeatRejectionPower,
    ControlRoomValueSnapshot SteamSpaceTemperature,
    ControlRoomValueSnapshot HotwellTemperature,
    string SteamSpacePhase,
    [property: JsonIgnore] ControlRoomValueSnapshot CondensateSpecificInternalEnergy,
    [property: JsonIgnore] ControlRoomValueSnapshot SpecificCondensationEnergyDrop,
    [property: JsonIgnore] string CondensationLimitStatus,
    [property: JsonIgnore] ControlRoomValueSnapshot InstalledCoolingCapacity,
    [property: JsonIgnore] ControlRoomValueSnapshot AvailableCoolingCapacity,
    [property: JsonIgnore] ControlRoomValueSnapshot SurfaceHeatTransferLimit,
    [property: JsonIgnore] string HeatRejectionLimitStatus);
