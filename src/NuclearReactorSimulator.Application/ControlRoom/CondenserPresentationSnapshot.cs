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
    string SteamSpacePhase);
