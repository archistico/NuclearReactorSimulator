using NuclearReactorSimulator.Application.ControlRoom;

namespace NuclearReactorSimulator.Application.ControlRoom.Hmi;

public sealed record ControlRoomPlantMimicConnectionSnapshot(
    string ConnectionId,
    string FromElementId,
    string ToElementId,
    ControlRoomPlantMimicMedium Medium,
    string MediumText,
    string PrimaryText,
    string SecondaryText,
    ControlRoomVisualState State,
    IReadOnlyList<ControlRoomPlantMimicPointSnapshot> Route,
    double LabelX,
    double LabelY);
