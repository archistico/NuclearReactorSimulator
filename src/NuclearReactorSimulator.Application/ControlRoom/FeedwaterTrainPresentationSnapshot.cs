namespace NuclearReactorSimulator.Application.ControlRoom;

public sealed record FeedwaterTrainPresentationSnapshot(
    string TrainId,
    string CondenserId,
    string FeedwaterTargetNodeId,
    SecondaryPumpPresentationSnapshot CondensatePump,
    SecondaryPumpPresentationSnapshot FeedwaterPump,
    ControlRoomValueSnapshot HotwellMass,
    ControlRoomValueSnapshot FeedwaterInventoryMass,
    ControlRoomValueSnapshot FeedwaterTemperature,
    ControlRoomValueSnapshot ThermalConditioningPower);
